using UnityEngine;

/// <summary>
/// 挂在终点线的 <see cref="Collider2D"/>（Is Trigger）上；仅当碰撞体属于 <see cref="carRoot"/> 时通知计圈。
/// 通过比较车辆速度方向与本节点的「前进轴」点乘，过滤反向冲线（轻量反作弊，未来由 checkpoint 替代）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RcCarFinishLine2D : MonoBehaviour
{
    /// <summary>赛道前进方向取本节点的哪个局部轴；摆放终点线时把该轴指向赛道前方。</summary>
    public enum ForwardAxis
    {
        LocalUp,
        LocalRight
    }

    [SerializeField] RcCarRaceSession2D session;
    [Tooltip("车辆的根节点（含 Rigidbody2D 的物体）；进入/离开触发器时与 other.transform 比对")]
    [SerializeField] Transform carRoot;

    [Header("方向判定")]
    [Tooltip("终点线节点的「赛道前进方向」取哪个局部轴；摆放时让该轴指向赛道前方")]
    [SerializeField] ForwardAxis forwardAxis = ForwardAxis.LocalUp;
    [Tooltip("车辆速度在前进轴上的分量需大于此阈值才算合法穿越（米/秒），用于过滤静止/极慢误触发")]
    [SerializeField] float minForwardSpeed = 0.1f;

    Collider2D _col;

    /// <summary>关卡加载后对 Session 与车辆根赋值（预制体上 session/carRoot 可先留空）。</summary>
    public void BindSessionAndCar(RcCarRaceSession2D s, Transform vehicleRoot)
    {
        session = s;
        carRoot = vehicleRoot;
    }

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (session == null || carRoot == null)
        {
            return;
        }
        if (!IsCarCollider(other))
        {
            return;
        }
        // 仅在车辆速度沿赛道前进方向时算作合法穿越；反向冲线 / 几乎静止被推过都忽略。
        if (!IsCrossingForward(other.attachedRigidbody))
        {
            return;
        }
        session.NotifyFinishEnterFromCar();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (session == null || carRoot == null)
        {
            return;
        }
        if (!IsCarCollider(other))
        {
            return;
        }
        session.NotifyFinishExitFromCar();
    }

    bool IsCarCollider(Collider2D other)
    {
        if (other.attachedRigidbody == null)
        {
            return false;
        }
        return other.attachedRigidbody.transform == carRoot;
    }

    bool IsCrossingForward(Rigidbody2D rb)
    {
        if (rb == null)
        {
            return false;
        }
        Vector2 forward = forwardAxis == ForwardAxis.LocalUp
            ? (Vector2)transform.up
            : (Vector2)transform.right;
        return Vector2.Dot(rb.linearVelocity, forward) > minForwardSpeed;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 forward = forwardAxis == ForwardAxis.LocalUp ? transform.up : transform.right;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + forward * 1.5f);
    }
#endif
}
