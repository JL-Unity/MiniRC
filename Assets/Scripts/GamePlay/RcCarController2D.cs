using UnityEngine;

// -----------------------------------------------------------------------------
// 2D 俯视遥控车（车头 = transform.up）
// · 读入：RcCarInputSystemPlayer（Move.x / Sprint / Reverse）+ 可选 UI Joystick.Horizontal
// · 速度：在车身坐标系 (前进, 侧向) 里做指数衰减，再叠加油门力与限速
// · 转向：直接写 Rigidbody2D.angularVelocity，单位「度/秒」（与 Unity API 一致）；低速满舵权优先（撞墙后好转，可略接受原地打舵）
// · 侧向：lateralRate 越大 → exp(-rate·dt) 越小 → 横速衰减越快 = 越抓地；直线用大 rate、弯里随舵角降到小 rate
// · 松手：舵角用 steerAngleReleaseSpeedDeg 快回中；转弯/侧滑用 min(平滑舵, 输入)，输入为 0 时立刻按直行算，避免松手还甩
// · 惯性：纵向靠 forwardDrag / coastExtraDrag × longitudinalDragScale；系数越小滑得越远。还可提高 Rigidbody.mass 并同比加大 accelerationForce 让加减速更「沉」
// -----------------------------------------------------------------------------

/// <summary>
/// 2D 俯视遥控车：车头 <see cref="transform.up"/>，侧向 <see cref="transform.right"/>。
/// 油门 Sprint/Reverse；转向 = Move.x + UI 摇杆 Horizontal。
/// <b>侧向抓地随舵角变化</b>：不转向时高抓地（稳）；舵越大侧向衰减越弱，弯里略带漂移感。
/// 角速度为街机式（度/秒）；转弯权重低速偏高（奥德赛式好修正），静止打满舵可略陀螺。松手后输入为 0 时物理舵幅立即归零，并加快回中与角速度收敛。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class RcCarController2D : MonoBehaviour
{
    [Header("References")]
    [Tooltip("本物体上的 Rigidbody2D；可留空由 Reset/Awake 自动拉取")]
    [SerializeField] Rigidbody2D rb;

    [Header("Drive · 沿车头加减速")]
    [Tooltip("油门为 ±1 时沿车头施加的力（世界单位下需结合质量调节体感）")]
    [SerializeField] float accelerationForce = 38f;
    [Tooltip("前进方向沿车头最大线速度（世界单位/秒）")]
    [SerializeField] float maxForwardSpeed = 9f;
    [Tooltip("倒车方向沿车头最大线速度（取正值，表示限速幅度）")]
    [SerializeField] float maxReverseSpeed = 4f;
    [Tooltip("倒车时 accelerationForce 乘的倍率（一般小于 1）")]
    [SerializeField] float reverseAcceRate = 0.55f;

    [Header("Steer · 街机式（angularVelocity = 度/秒）")]
    [Tooltip("横向输入打满 [-1,1] 映射到的舵角幅度（度）；同时供 UI 轮胎旋转")]
    [SerializeField] float maxSteerAngleDeg = 180f;
    [Tooltip("打舵时舵角向目标靠近的变化率（度/秒）")]
    [SerializeField] float steerAngleFollowSpeedDeg = 480f;
    [Tooltip("松手回中时舵角向 0 靠近的变化率（宜远大于上面，否则松手还会按大舵转一会儿、侧滑也收不住）")]
    [SerializeField] float steerAngleReleaseSpeedDeg = 1100f;
    [Tooltip("横向打满时节流的角速度上限（度/秒），与 Rigidbody2D.angularVelocity 单位一致")]
    [SerializeField] float maxYawRateDeg = 180f;
    [Tooltip("参考速度：vMeasure 从 0 插值到该值时，转弯权重从 AtRest 过渡到 AtRefSpeed")]
    [SerializeField] float steerSpeedForFullTurn = 2f;
    [Tooltip("vMeasure≈0 时的转弯权重（1=低速即满舵权，撞墙后好转；略降可减轻原地陀螺）")]
    [SerializeField] float steerTurnFactorAtRest = 1f;
    [Tooltip("vMeasure ≥ steerSpeedForFullTurn 时的转弯权重；略 <1 可让高速略稳")]
    [SerializeField] float steerTurnFactorAtRefSpeed = 1f;
    [Tooltip("油门对折向权重的额外贡献（在转向有效时叠加）")]
    [SerializeField] float steerThrottleTurnWeight = 0.35f;
    [Tooltip("有转向输入时，角速度向目标逼近的快慢（exp(-steerOmegaGain·dt)）")]
    [SerializeField] float steerOmegaGain = 12f;
    [Tooltip("横向输入已回中（视为松手）时，额外把角速度往目标拉的强度，越大松手越-stop 转")]
    [SerializeField] float steerReleaseYawGain = 26f;

    [Header("Slip · 直稳弯漂（lateralRate 大 = 横速衰得快 = 抓地牢）")]
    [Tooltip("几乎不转向时的侧向衰减系数；宜大，直线稳")]
    [SerializeField] float lateralGripStraight = 14f;
    [Tooltip("舵接近打满时的侧向衰减系数；宜明显小于 Straight，弯里才容易存横滑")]
    [SerializeField] float lateralGripTurning = 5f;
    [Tooltip("在 Straight~Turning 间按 |舵| 的该次方插值；>1 则小舵仍偏直、大舵才明显滑")]
    [SerializeField] float lateralSteerSlipPower = 1.4f;
    [Tooltip("带油门且正在转向时，额外削弱侧向衰减（略增甩尾）；0 关闭")]
    [SerializeField] float lateralThrottleDrift = 2.2f;
    [Tooltip("纵轴 v∥ 每帧 exp(-forwardDrag·dt)；略小则松油门后少拖长滑行")]
    [SerializeField] float forwardDrag = 2.3f;
    [Tooltip("无油门时纵轴再乘 exp(-coastExtraDrag·dt)")]
    [SerializeField] float coastExtraDrag = 0.38f;

    [Header("Inertia · 纵向滑行")]
    [Tooltip("乘在 forwardDrag 与 coastExtraDrag 上：<1 纵向衰减慢、松油后更耐滑；>1 更快停。不改侧向抓地")]
    [SerializeField] [Range(0.25f, 1.35f)] float longitudinalDragScale = 0.82f;

    [Header("Collision · 贴墙补丁（抑制撞墙后沿墙滑）")]
    [Tooltip("OnCollisionStay 刷新后保持多少秒「贴墙态」；过期自动失效，无需依赖 Exit 事件配对")]
    [SerializeField] float wallContactWindow = 0.05f;
    [Tooltip("贴墙期间 lateralRate 的下限；≤0 关闭此补丁；默认 ≈ lateralGripStraight，可略高以更快消滑")]
    [SerializeField] float wallContactLateralGripFloor = 14f;

    [Header("Brake")]
    [Tooltip("油门与当前沿车头速度方向相反时的附加制动力")]
    [SerializeField] float brakeForce = 50f;

    [Header("Rigidbody")]
    [Tooltip("高速易穿模时可开 Continuous")]
    [SerializeField] bool continuousCollision = true;
    [Tooltip("2D 俯视一般为 0")]
    [SerializeField] float defaultGravityScale = 0f;
    [Tooltip("刚体线性阻尼；本脚本主要手写速度衰减，此项常为 0")]
    [SerializeField] float rigidbodyLinearDrag = 0f;
    [Tooltip("刚体角阻尼；过大会抵消脚本设置的角速度")]
    [SerializeField] float rigidbodyAngularDrag = 0.2f;

    [Header("Debug")]
    [Tooltip("每个 FixedUpdate 输出一次速度日志：基于位置差/dt 的实诚速度 + rb.linearVelocity 对照。仅调试用，验完请关；走 LogClass 仅 Editor 输出")]
    [SerializeField] bool enableSpeedLog;

    [Header("Input")]
    [Tooltip("绑定 Input Actions 里 Player 的 Move / Sprint / Reverse")]
    [SerializeField] RcCarInputSystemPlayer inputPlayer;
    [Tooltip("Joystick Pack 屏幕摇杆；Horizontal 并进横向")]
    [SerializeField] Joystick uiJoystick;
    [Tooltip("关闭则不读 Move（键盘 A/D 等）；纯触屏可关、只用手柄摇杆")]
    [SerializeField] bool includePhysicalMoveInput = true;
    [Tooltip("横向输入绝对值小于此当作 0")]
    [SerializeField] float inputDeadZone = 0.08f;

    /// <summary>当前平滑后的舵角（度），相对车身；前轮胎体可跟转。</summary>
    public float CurrentSteerAngleDeg => _steerAngleDeg;

    /// <summary>平滑后的目标舵角（度），由横向输入驱动。</summary>
    float _steerAngleDeg;

    // OnCollisionStay2D 在 FixedUpdate 之后由物理系统触发，所以这里存的是「上一个物理步的接触状态」，>0 视为贴墙中
    float _wallContactTimer;

    // 速度日志用：上一 FixedUpdate 开头的 rb.position；本帧位移除以 dt 即"上一物理 Step 的真实位移速度"
    Vector2 _prevPos;
    bool _prevPosValid;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (inputPlayer == null)
        {
            inputPlayer = GetComponent<RcCarInputSystemPlayer>();
        }
    }

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (inputPlayer == null)
        {
            inputPlayer = GetComponent<RcCarInputSystemPlayer>();
        }

        rb.gravityScale = defaultGravityScale;
        rb.linearDamping = rigidbodyLinearDrag;
        rb.angularDamping = rigidbodyAngularDrag;
        rb.collisionDetectionMode = continuousCollision
            ? CollisionDetectionMode2D.Continuous
            : CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // 确保场景里若曾勾选 Freeze Rotation Z，此处仍会允许转弯
        rb.constraints = RigidbodyConstraints2D.None;
    }

    /// <summary>任何 2D 碰撞体只要持续接触，每物理步都会触发一次；用来刷新「贴墙中」定时器。</summary>
    void OnCollisionStay2D(Collision2D _) => _wallContactTimer = wallContactWindow;

    /// <summary>每物理步：汇总输入 → 更新舵角 → 跑一圈车体力学。</summary>
    void FixedUpdate()
    {
        // --- 横向 [-1,1]：新 Input System 的 Move.x + UI 摇杆 ---
        float steerX = 0f;
        if (includePhysicalMoveInput && inputPlayer != null)
        {
            steerX += inputPlayer.ReadMove().x;
        }
        if (uiJoystick != null)
        {
            steerX += uiJoystick.Horizontal;
        }

        steerX = Mathf.Clamp(steerX, -1f, 1f);
        if (Mathf.Abs(steerX) < inputDeadZone)
        {
            steerX = 0f;
        }

        // --- 油门：Sprint / Reverse 互斥；同按或都不按为 0 ---
        bool sprint = inputPlayer != null && inputPlayer.ReadSprint();
        bool rev = inputPlayer != null && inputPlayer.ReadReverse();
        float throttle = 0f;
        if (sprint && !rev)
        {
            throttle = 1f;
        }
        else if (rev && !sprint)
        {
            throttle = -1f;
        }

        float dt = Time.fixedDeltaTime;
        _wallContactTimer = Mathf.Max(0f, _wallContactTimer - dt);
        float targetSteerDeg = steerX * maxSteerAngleDeg;
        // 松手回中用更快回中，避免 _steerAngleDeg 滞后导致仍按大舵转弯/漂移
        float steerFollow = Mathf.Abs(steerX) < 0.001f ? steerAngleReleaseSpeedDeg : steerAngleFollowSpeedDeg;
        _steerAngleDeg = Mathf.MoveTowards(_steerAngleDeg, targetSteerDeg, steerFollow * dt);

        // 在 RunPhysics 之前打日志：此时 rb.linearVelocity 与 (rb.position - _prevPos)/dt 都是"上一 Step 后的产物"，口径一致便于对照
        if (enableSpeedLog) LogSpeedFrame(throttle, dt);
        // 缓存维护放在 if 之外：开关切换后下一帧也能立刻有正确的 _prevPos
        _prevPos = rb.position;
        _prevPosValid = true;

        RunPhysics(throttle, maxForwardSpeed, dt, steerX);
    }

    /// <summary>
    /// 每物理步打一行速度数据。两组对照：
    /// · posVel：基于 (rb.position - _prevPos)/dt——transform 实际位移的"实诚速度"，不受任何 velocity 赋值或排队 force 影响
    /// · rb：直读 rb.linearVelocity，反映"上一 Step 后写入 Rigidbody 的速度"
    /// 稳态两者应基本一致；明显偏离说明有 contact 解算或外部脚本干预 transform/velocity。
    /// </summary>
    void LogSpeedFrame(float throttle, float dt)
    {
        Vector2 fwd = transform.up;
        Vector2 v = rb.linearVelocity;
        float rbFwd = Vector2.Dot(v, fwd);

        float posVelFwd = 0f;
        float posVelMag = 0f;
        if (_prevPosValid)
        {
            Vector2 disp = rb.position - _prevPos;
            posVelFwd = Vector2.Dot(disp, fwd) / dt;
            posVelMag = disp.magnitude / dt;
        }

        LogClass.LogGame(GameLogCategory.RcCar,
            $"speed posVel.fwd={posVelFwd:F3} mag={posVelMag:F3} | rb.fwd={rbFwd:F3} mag={v.magnitude:F3} | cap={maxForwardSpeed:F2} accF={accelerationForce:F0} mass={rb.mass:F2} throttle={throttle:F1}");
    }

    /// <summary>
    /// 先限速，再在车身系里对速度做指数衰减、施力，最后按舵角与车速写角速度。
    /// <paramref name="steerInput"/>：经死区后的横向输入；转弯/抓地用 min(平滑舵, 输入)，松手为 0 即不再按弯里算。
    /// </summary>
    void RunPhysics(float throttle, float forwardSpeedCap, float dt, float steerInput)
    {
        ClampSpeedAlongForward(forwardSpeedCap);

        Vector2 forward = transform.up;
        Vector2 right = transform.right;

        Vector2 v = rb.linearVelocity;
        // 世界速度投到当前车头的前/右轴
        float forwardVel = Vector2.Dot(v, forward);
        float lateralVel = Vector2.Dot(v, right);

        // --- 侧向抓地：物理舵幅 = min(|平滑舵|, |输入|)。松手输入=0 → 立刻按直路抓地，不收滑到撞墙 ---
        float steerNormPhysics = SteerNormForPhysics(steerInput);
        float steerAbsForGrip = Mathf.Abs(steerNormPhysics);
        float slipBlend = Mathf.Pow(steerAbsForGrip, lateralSteerSlipPower);
        float lateralRate = Mathf.Lerp(lateralGripStraight, lateralGripTurning, slipBlend);
        if (Mathf.Abs(throttle) > 0.01f && steerAbsForGrip > 0.08f)
        {
            lateralRate -= lateralThrottleDrift * steerAbsForGrip * Mathf.Abs(throttle);
        }
        // 防止 rate 过低导致数值过于「滑冰」或不稳定
        lateralRate = Mathf.Max(2.5f, lateralRate);

        // 贴墙补丁：碰撞会把原本的纵向动能经 Dot 分解到 lateralVel（斜贴墙），弯道低抓地档会让这份「被动横速」
        // 衰得很慢 → 沿墙滑一大截。此处把 lateralRate 抬到 straight 档，快速吃掉这部分横速。
        if (_wallContactTimer > 0f && wallContactLateralGripFloor > 0f)
        {
            lateralRate = Mathf.Max(lateralRate, wallContactLateralGripFloor);
        }

        // v 分量每步乘 exp(-rate·dt)，等效连续阻尼；纵向再乘 longitudinalDragScale 调「惯性滑行」
        float lateralFactor = Mathf.Exp(-lateralRate * dt);
        float fwdD = forwardDrag * longitudinalDragScale;
        float coastD = coastExtraDrag * longitudinalDragScale;
        float forwardFactor = Mathf.Exp(-fwdD * dt);
        if (Mathf.Abs(throttle) < 0.01f)
        {
            forwardFactor *= Mathf.Exp(-coastD * dt);
        }

        lateralVel *= lateralFactor;
        forwardVel *= forwardFactor;
        rb.linearVelocity = forward * forwardVel + right * lateralVel;

        float forwardAfterDamp = Vector2.Dot(rb.linearVelocity, forward);

        float accelMul = throttle >= 0f ? 1f : reverseAcceRate;
        float driveForce = throttle * accelerationForce * accelMul;
        if (Mathf.Abs(driveForce) > 0f)
        {
            // 预算式施力：物理 Step 会把 driveForce 转成 Δv = F·dt/m 加到 linearVelocity 上。
            // 施力前算"沿车头还能涨多少 Δv 不超 cap"，按比例把 driveForce 缩到余量内；
            float mass = Mathf.Max(1e-4f, rb.mass);
            float wantedDeltaV = driveForce * dt / mass;
            bool sameDir = (throttle > 0f && forwardAfterDamp >= 0f)
                        || (throttle < 0f && forwardAfterDamp <= 0f);
            if (sameDir)
            {
                float cap = throttle >= 0f ? forwardSpeedCap : maxReverseSpeed;
                float headroom = Mathf.Max(0f, cap - Mathf.Abs(forwardAfterDamp));
                if (Mathf.Abs(wantedDeltaV) > headroom)
                {
                    driveForce *= headroom / Mathf.Abs(wantedDeltaV);
                }
            }
            rb.AddForce(forward * driveForce);
        }

        // 油门与当前车头速度反向：额外刹车感
        if (Mathf.Abs(throttle) > 0.01f)
        {
            float oppose = Mathf.Sign(throttle) * Mathf.Sign(forwardAfterDamp);
            if (oppose < 0f && Mathf.Abs(forwardAfterDamp) > 0.05f)
            {
                rb.AddForce(-Mathf.Sign(forwardAfterDamp) * forward * brakeForce);
            }
        }

        // --- 转向：同样用 steerNormPhysics，松手不再持续施加弯心角速度 ---
        float steerNorm = steerNormPhysics;
        float speedMag = rb.linearVelocity.magnitude;
        float along = Mathf.Abs(forwardAfterDamp);
        float vMeasure = Mathf.Max(along, speedMag);

        float turnFactor = 0f;
        if (Mathf.Abs(steerNorm) > 1e-5f)
        {
            float speedT = Mathf.Clamp01(vMeasure / Mathf.Max(0.01f, steerSpeedForFullTurn));
            turnFactor = Mathf.Lerp(steerTurnFactorAtRest, steerTurnFactorAtRefSpeed, speedT);
            turnFactor += steerThrottleTurnWeight * Mathf.Abs(throttle);
            turnFactor = Mathf.Clamp01(turnFactor);
        }

        float desiredYawDegPerSec = -steerNorm * maxYawRateDeg * turnFactor;
        float t = Mathf.Clamp01(1f - Mathf.Exp(-steerOmegaGain * dt));
        // 松手：更强地把角速度拉向目标（多为 0），减少残余旋转
        if (Mathf.Abs(steerInput) < inputDeadZone)
        {
            t = Mathf.Max(t, Mathf.Clamp01(1f - Mathf.Exp(-steerReleaseYawGain * dt)));
        }
        float yaw = Mathf.Lerp(rb.angularVelocity, desiredYawDegPerSec, t);
        rb.angularVelocity = Mathf.Clamp(yaw, -maxYawRateDeg, maxYawRateDeg);

    }

    /// <summary>
    /// 平滑舵与实际输入同时参与：幅值取 min(|平滑|, |输入|)；有输入时方向跟输入，松手后输入为 0 → 立刻为 0。
    /// </summary>
    float SteerNormForPhysics(float steerInput)
    {
        float sm = Mathf.Clamp(_steerAngleDeg / Mathf.Max(1f, maxSteerAngleDeg), -1f, 1f);
        float inp = Mathf.Clamp(steerInput, -1f, 1f);
        float mag = Mathf.Min(Mathf.Abs(sm), Mathf.Abs(inp));
        if (mag < 1e-5f)
        {
            return 0f;
        }
        float sign = Mathf.Abs(inp) > 1e-5f ? Mathf.Sign(inp) : Mathf.Sign(sm);
        return sign * mag;
    }

    /// <summary>沿车头方向的分速度不应超过前进/倒车各自上限。</summary>
    void ClampSpeedAlongForward(float forwardSpeedCap)
    {
        Vector2 v = rb.linearVelocity;
        Vector2 forward = transform.up;
        float fwd = Vector2.Dot(v, forward);
        float limit = fwd >= 0f ? forwardSpeedCap : maxReverseSpeed;
        if (Mathf.Abs(fwd) > limit)
        {
            float excess = Mathf.Abs(fwd) - limit;
            v -= forward * Mathf.Sign(fwd) * excess;
            rb.linearVelocity = v;
        }
    }
}
