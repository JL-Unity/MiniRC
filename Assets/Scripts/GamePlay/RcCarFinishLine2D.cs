using UnityEngine;

/// <summary>
/// 挂在终点线的 <see cref="Collider2D"/>（Is Trigger）上；仅当碰撞体属于 <see cref="carRoot"/> 时通知计圈。
/// 反向冲线 / 绕路捷径由 Session 上的 <see cref="RcCarMidCheckpoint2D"/> 守卫拦截，本组件不再判定方向。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RcCarFinishLine2D : MonoBehaviour
{
    [SerializeField] RcCarRaceSession2D session;
    [Tooltip("车辆的根节点（含 Rigidbody2D 的物体）；进入/离开触发器时与 other.transform 比对")]
    [SerializeField] Transform carRoot;

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
}
