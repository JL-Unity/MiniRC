using UnityEngine;

/// <summary>
/// 挂在终点线的 <see cref="Collider2D"/>（Is Trigger）上；仅当碰撞体属于 <see cref="carRoot"/> 时通知计圈。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RcCarFinishLine2D : MonoBehaviour
{
    [SerializeField] RcCarRaceSession2D session;
    [Tooltip("车辆的根节点（含 Rigidbody2D 的物体）；进入/离开触发器时与 other.transform 比对")]
    [SerializeField] Transform carRoot;

    Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (session == null || carRoot == null)
            return;
        if (!IsCarCollider(other))
            return;
        session.NotifyFinishEnterFromCar();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (session == null || carRoot == null)
            return;
        if (!IsCarCollider(other))
            return;
        session.NotifyFinishExitFromCar();
    }

    bool IsCarCollider(Collider2D other)
    {
        if (other.attachedRigidbody == null)
            return false;
        return other.attachedRigidbody.transform == carRoot;
    }
}
