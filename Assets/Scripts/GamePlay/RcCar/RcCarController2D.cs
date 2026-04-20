using UnityEngine;

/// <summary>
/// 2D 俯视角遥控车：车头 <see cref="transform.up"/>。
/// Sprint=油门，Reverse=倒车；左右输入为<b>相对车身</b>的「方向盘角」指令（非世界绝对朝向）。
/// 转弯角速度随<b>车速</b>升高，近似 RC：静止时轮子可打角但车身几乎不绕圈自转；纵/侧速度分别衰减产生侧滑。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class RcCarController2D : MonoBehaviour
{
    [Header("References")]
    [Tooltip("本物体 Rigidbody2D；未指定则 Reset/Awake 自动获取")]
    [SerializeField] Rigidbody2D rb;

    [Header("Drive")]
    [Tooltip("油门 ±1 时沿车头的推力")]
    [SerializeField] float accelerationForce = 38f;
    [Tooltip("前进沿车头最大速度")]
    [SerializeField] float maxForwardSpeed = 9f;
    [Tooltip("倒车沿车头最大速度")]
    [SerializeField] float maxReverseSpeed = 4f;
    [Tooltip("倒车推力相对前进的倍率")]
    [SerializeField] float reverseAccelerationMultiplier = 0.55f;

    [Header("Steer（相对车身的方向盘角 → 角速度）")]
    [Tooltip("左右输入每秒向目标方向盘角靠近的幅度（-1～1 量纲）")]
    [SerializeField] float steerWheelFollowSpeed = 4f;
    [Tooltip("纯靠车速时，线速度到多少转弯接近满；宜偏小，否则低速几乎转不动")]
    [SerializeField] float steerSpeedForFullTurn = 0.9f;
    [Tooltip("有油门时额外增加转弯权（0～1），解决低速/刚起步转不动")]
    [SerializeField] float steerThrottleAssist = 0.38f;
    [Tooltip("打舵时微量低速转弯权，避免完全死转；不宜过大以免原地陀螺")]
    [SerializeField] float steerWheelLowSpeedAssist = 0.1f;
    [Tooltip("满舵时最大转弯角速度（度/秒）")]
    [SerializeField] float maxYawRateDeg = 200f;
    [Tooltip("当前角速度向目标角速度靠近的快慢，越大越跟手")]
    [SerializeField] float steerOmegaGain = 14f;

    [Header("Slip")]
    [Tooltip("侧向速度每帧 exp(-本值*dt)")]
    [SerializeField] float lateralGrip = 4.2f;
    [Tooltip("纵向速度每帧 exp(-本值*dt)")]
    [SerializeField] float forwardDrag = 2.8f;
    [Tooltip("无油门时额外纵向衰减（越小惯性滑行越久）")]
    [SerializeField] float coastExtraDrag = 0.55f;

    [Header("Brake")]
    [Tooltip("油门与沿车头速度反向时的额外制动力")]
    [SerializeField] float brakeForce = 50f;

    [Header("Rigidbody")]
    [Tooltip("高速建议 Continuous")]
    [SerializeField] bool continuousCollision = true;
    [Tooltip("俯视角一般为 0")]
    [SerializeField] float defaultGravityScale = 0f;
    [SerializeField] float rigidbodyLinearDrag = 0f;
    [Tooltip("每帧会削弱角速度；本脚本会主动设角速度，过大易导致「转不动」，建议 0～0.3")]
    [SerializeField] float rigidbodyAngularDrag = 0.15f;

    [Header("Input")]
    [SerializeField] RcCarInputSystemPlayer inputPlayer;
    [Tooltip("仅左右=方向盘；纵轴不用")]
    [SerializeField] Joystick uiJoystick;
    [Tooltip("关则只用 UI 摇杆，不读实体 Move")]
    [SerializeField] bool includePhysicalMoveInput = true;
    [SerializeField] float inputDeadZone = 0.08f;

    [Header("Debug")]
    [Tooltip("油门(Sprint)按下/松开时打日志")]
    [SerializeField] bool logThrottleToConsole;
    bool _loggedSprintPrev;

    /// <summary>相对车身的方向盘角 -1～1，由左右输入平滑得到。</summary>
    float _steerWheel;

    float MaxYawRateRad => maxYawRateDeg * Mathf.Deg2Rad;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (inputPlayer == null)
            inputPlayer = GetComponent<RcCarInputSystemPlayer>();
    }

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (inputPlayer == null)
            inputPlayer = GetComponent<RcCarInputSystemPlayer>();

        rb.gravityScale = defaultGravityScale;
        rb.linearDamping = rigidbodyLinearDrag;
        rb.angularDamping = rigidbodyAngularDrag;
        rb.collisionDetectionMode = continuousCollision
            ? CollisionDetectionMode2D.Continuous
            : CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.None;

        if (inputPlayer != null)
            _loggedSprintPrev = inputPlayer.ReadSprint();
    }

    void FixedUpdate()
    {
        Vector2 move = Vector2.zero;
        if (includePhysicalMoveInput && inputPlayer != null)
        {
            Vector2 m = inputPlayer.ReadMove();
            move.x = m.x;
        }

        if (uiJoystick != null && uiJoystick.Direction.sqrMagnitude > 0.0001f)
            move.x += uiJoystick.Direction.x;

        float steerInput = Mathf.Clamp(move.x, -1f, 1f);
        if (Mathf.Abs(steerInput) < inputDeadZone)
            steerInput = 0f;

        bool sprint = inputPlayer != null && inputPlayer.ReadSprint();
        bool rev = inputPlayer != null && inputPlayer.ReadReverse();

        float throttle;
        if (sprint && !rev)
            throttle = 1f;
        else if (rev && !sprint)
            throttle = -1f;
        else
            throttle = 0f;

        if (logThrottleToConsole && sprint != _loggedSprintPrev)
        {
            Debug.Log($"[RcCar] 油门(Sprint)={(sprint ? "ON" : "OFF")} throttle={throttle:F2}", this);
            _loggedSprintPrev = sprint;
        }

        float dt = Time.fixedDeltaTime;
        _steerWheel = Mathf.MoveTowards(_steerWheel, steerInput, steerWheelFollowSpeed * dt);

        RunPhysics(throttle, maxForwardSpeed, dt);
    }

    void RunPhysics(float throttle, float forwardSpeedCap, float dt)
    {
        ClampSpeedAlongForward(forwardSpeedCap);

        Vector2 forward = transform.up;
        Vector2 right = transform.right;

        Vector2 v = rb.linearVelocity;
        float forwardVel = Vector2.Dot(v, forward);
        float lateralVel = Vector2.Dot(v, right);

        float lateralFactor = Mathf.Exp(-lateralGrip * dt);
        float forwardFactor = Mathf.Exp(-forwardDrag * dt);
        if (Mathf.Abs(throttle) < 0.01f)
            forwardFactor *= Mathf.Exp(-coastExtraDrag * dt);

        lateralVel *= lateralFactor;
        forwardVel *= forwardFactor;
        rb.linearVelocity = forward * forwardVel + right * lateralVel;

        float forwardAfterDamp = Vector2.Dot(rb.linearVelocity, forward);

        float accelMul = throttle >= 0f ? 1f : reverseAccelerationMultiplier;
        rb.AddForce(forward * (throttle * accelerationForce * accelMul));

        if (Mathf.Abs(throttle) > 0.01f)
        {
            float oppose = Mathf.Sign(throttle) * Mathf.Sign(forwardAfterDamp);
            if (oppose < 0f && Mathf.Abs(forwardAfterDamp) > 0.05f)
                rb.AddForce(-Mathf.Sign(forwardAfterDamp) * forward * brakeForce);
        }

        // 舵角 → 目标角速度：随车速升高（防原地陀螺），并用油门/微量舵角补低速，避免「完全转不动」
        float speed = rb.linearVelocity.magnitude;
        float turnFactor = Mathf.Clamp01(speed / Mathf.Max(0.01f, steerSpeedForFullTurn));
        turnFactor += Mathf.Abs(throttle) * steerThrottleAssist;
        turnFactor += Mathf.Abs(_steerWheel) * steerWheelLowSpeedAssist;
        turnFactor = Mathf.Clamp01(turnFactor);

        float desiredOmega = -_steerWheel * MaxYawRateRad * turnFactor;
        float t = Mathf.Clamp01(1f - Mathf.Exp(-steerOmegaGain * dt));
        float omega = Mathf.Lerp(rb.angularVelocity, desiredOmega, t);
        rb.angularVelocity = Mathf.Clamp(omega, -MaxYawRateRad, MaxYawRateRad);
    }

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
