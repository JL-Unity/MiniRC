using UnityEngine;

/// <summary>
/// 挂在关卡根预制体上：提供小车出生点；可选终点触发器供 Session 绑定。
/// 可在子物体下放置名为 CarSpawn 的空物体；否则使用序列化引用。
/// </summary>
[DisallowMultipleComponent]
public class RcRaceLevelRoot : MonoBehaviour
{
    [SerializeField] Transform carSpawn;
    [Tooltip("可选：终点触发 Collider2D（Is Trigger），将交给 RcCarRaceSession2D")]
    [SerializeField] Collider2D finishTrigger;

    void Awake()
    {
        ResolveCarSpawn();
    }

    void ResolveCarSpawn()
    {
        if (carSpawn != null)
            return;
        var t = transform.Find("CarSpawn");
        if (t != null)
            carSpawn = t;
    }

    public Transform CarSpawn
    {
        get
        {
            ResolveCarSpawn();
            return carSpawn;
        }
    }

    public Collider2D FinishTrigger => finishTrigger;

    public RcCarFinishLine2D[] GetFinishLinesInLevel()
    {
        return GetComponentsInChildren<RcCarFinishLine2D>(true);
    }
}
