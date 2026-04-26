using UnityEngine;

// -----------------------------------------------------------------------------
// 胎痕发射器：挂在车预制体上，按侧向速度判断是否"漂"，两个后轮各维护一段"活动 LineRenderer"
// · 漂中：轮子移动超过 minPointDistance 就往当前段追加一个点 —— 天然连续曲线
// · 漂停：把当前段 StartFade 切出去，它自己进入 hold→fade→归还池
// · 段点数过多：直接切出当前段 + 新开一段（首点接上次落点避免视觉断开），防止 LineRenderer 点数无限膨胀
// · 与 RcCarController2D 解耦：自己从 Rigidbody2D.linearVelocity 投影到 transform.right 读侧向速度
// -----------------------------------------------------------------------------

[DisallowMultipleComponent]
public class RcCarSkidEmitter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("车的 Rigidbody2D；留空自动从自身获取")]
    [SerializeField] Rigidbody2D rb;
    [Tooltip("左后轮挂点（空物体即可）；胎痕点就从这个位置采样")]
    [SerializeField] Transform leftRearWheel;
    [Tooltip("右后轮挂点")]
    [SerializeField] Transform rightRearWheel;

    [Header("Pool")]
    [Tooltip("Resources/ 下的胎痕段预制体名（例如放在 Resources/Effects/SkidLine.prefab 就填 Effects/SkidLine）")]
    [SerializeField] string skidPrefabName = "Effects/SkidLine";

    // 所有段归到这个父物体下（场景内残留便于统一清理）。
    // 预制体不能序列化场景引用，所以由场景里的 RcCarRaceGameMode 在实例化车后调 SetSegmentParent 注入。
    Transform _segmentParent;

    /// <summary>由 GameMode 在实例化车后调用，指定胎痕段在 Hierarchy 里的父物体。</summary>
    public void SetSegmentParent(Transform parent) => _segmentParent = parent;

    [Header("Emit Condition · 触发条件")]
    [Tooltip("侧向速度 |v·right| 大于此阈值才视为在漂（m/s）")]
    [SerializeField] float lateralSpeedThreshold = 2.5f;
    [Tooltip("总速度（|v|）低于该值不生成；避免原地打舵刷一堆。用总速度而非车头方向分速，极端横滑时车头速分量会接近 0 导致胎痕中断")]
    [SerializeField] float minSpeedToEmit = 1.0f;
    [Tooltip("轮子移动超过该距离才追加一个点（世界单位）；过小点数暴涨、过大曲线会有折角")]
    [SerializeField] float minPointDistance = 0.08f;
    [Tooltip("单段最大点数；到顶就切出去新开一段，防止长漂积点")]
    [SerializeField] int maxPointsPerSegment = 200;

    // 每个后轮的活动段状态
    class WheelState
    {
        public Transform wheel;
        public RcCarSkidLine2D activeSegment;
        public Vector3 lastPoint;
    }

    readonly WheelState _left = new WheelState();
    readonly WheelState _right = new WheelState();

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        _left.wheel = leftRearWheel;
        _right.wheel = rightRearWheel;
    }

    // 用 LateUpdate：车辆物理步完成、Transform 同步到最新再采样轮位
    void LateUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 v = rb.linearVelocity;
        float lateral = Mathf.Abs(Vector2.Dot(v, transform.right));
        float speed = v.magnitude;
        bool drifting = lateral >= lateralSpeedThreshold && speed >= minSpeedToEmit;

        Process(_left, drifting);
        Process(_right, drifting);
    }

    void Process(WheelState s, bool drifting)
    {
        if (s.wheel == null)
        {
            return;
        }

        if (!drifting)
        {
            // 漂停：当前段切出去自淡出，下次起漂会取新段
            if (s.activeSegment != null)
            {
                s.activeSegment.StartFade();
                s.activeSegment = null;
            }
            return;
        }

        Vector3 cur = s.wheel.position;

        // 没有活动段 → 从池取一个新段并写入首点
        if (s.activeSegment == null)
        {
            s.activeSegment = AcquireSegment(cur);
            if (s.activeSegment == null)
            {
                return;
            }
            s.activeSegment.Line.positionCount = 1;
            s.activeSegment.Line.SetPosition(0, cur);
            s.lastPoint = cur;
            return;
        }

        // 距离不够不追加，保持点数可控
        if ((cur - s.lastPoint).sqrMagnitude < minPointDistance * minPointDistance)
        {
            return;
        }

        LineRenderer lr = s.activeSegment.Line;

        // 段点数到顶：切出去 + 新开，首点用上一段的末点避免视觉断裂
        if (lr.positionCount >= maxPointsPerSegment)
        {
            s.activeSegment.StartFade();
            RcCarSkidLine2D next = AcquireSegment(cur);
            if (next == null)
            {
                s.activeSegment = null;
                return;
            }
            next.Line.positionCount = 2;
            next.Line.SetPosition(0, s.lastPoint);
            next.Line.SetPosition(1, cur);
            s.activeSegment = next;
            s.lastPoint = cur;
            return;
        }

        int n = lr.positionCount;
        lr.positionCount = n + 1;
        lr.SetPosition(n, cur);
        s.lastPoint = cur;
    }

    RcCarSkidLine2D AcquireSegment(Vector3 spawnPos)
    {
        GameObject obj = PoolManager.GetInstance().GetObject(skidPrefabName, spawnPos);
        if (obj == null)
        {
            return null;
        }
        obj.transform.rotation = Quaternion.identity;
        if (_segmentParent != null)
        {
            obj.transform.SetParent(_segmentParent, true);
        }

        RcCarSkidLine2D line = obj.GetComponent<RcCarSkidLine2D>();
        if (line != null)
        {
            line.SetPoolKey(skidPrefabName);
        }
        return line;
    }
}
