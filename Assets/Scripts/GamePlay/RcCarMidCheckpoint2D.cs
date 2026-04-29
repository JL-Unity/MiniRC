using UnityEngine;

/// <summary>
/// 赛道中段反作弊检查点：玩家车辆触发后通知 <see cref="RcCarRaceSession2D"/>。
/// Session 要求当圈集齐所有 checkpoint 后，过终点线才计圈，避免反向冲线 / 绕路捷径。
/// 一关里 <see cref="checkpointId"/> 必须互不相同；触发顺序无关、重复触发自动去重。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RcCarMidCheckpoint2D : MonoBehaviour
{
    RcCarRaceSession2D _session;
    Transform _carRoot;

    [Header("标识")]
    [Tooltip("同关卡内必须唯一；Session 用 id 做去重")]
    [SerializeField] int checkpointId = 0;

    Collider2D _col;

    public int CheckpointId => checkpointId;

    /// <summary>关卡加载后由 GameMode 注入；预制体上 session/carRoot 可先留空。</summary>
    public void BindSessionAndCar(RcCarRaceSession2D s, Transform vehicleRoot)
    {
        _session = s; 
        _carRoot = vehicleRoot;
    }

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_session == null || _carRoot == null)
        {
            return;
        }
        if (!IsCarCollider(other))
        {
            return;
        }
        _session.NotifyMidpointTouched(checkpointId);
    }

    bool IsCarCollider(Collider2D other)
    {
        if (other.attachedRigidbody == null)
        {
            return false;
        }
        return other.attachedRigidbody.transform == _carRoot;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.9f);

        // 优先按 EdgeCollider2D 形状画两端点；折线/曲线也只标首尾，避免与编辑器自带的边线 Gizmo 重复。
        var edge = GetComponent<EdgeCollider2D>();
        if (edge != null && edge.pointCount >= 2)
        {
            Vector2 localStart = edge.points[0] + edge.offset;
            Vector2 localEnd = edge.points[edge.pointCount - 1] + edge.offset;
            Vector3 worldStart = transform.TransformPoint(localStart);
            Vector3 worldEnd = transform.TransformPoint(localEnd);
            Gizmos.DrawSphere(worldStart, 0.2f);
            Gizmos.DrawSphere(worldEnd, 0.2f);
            Gizmos.DrawLine(worldStart, worldEnd);
            return;
        }

        // Fallback：未挂 EdgeCollider2D 时给个十字 + 圆圈，至少能看到位置。
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.6f, transform.position + Vector3.right * 0.6f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.6f, transform.position + Vector3.up * 0.6f);
    }
#endif
}
