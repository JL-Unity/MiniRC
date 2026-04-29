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

    [Header("摄像机")]
    [Tooltip("进入本关时由 GameMode 写入赛车用 Camera.orthographicSize（须为正交相机）")]
    [SerializeField] float levelOrthographicSize = 5f;

    [Header("规则")]
    [Tooltip("本关总圈数，驱动 HUD 圈数行数量与 Session 计时终点判定")]
    [SerializeField, Range(1, 5)] int lapCount = 3;

    void Awake()
    {
        ResolveCarSpawn();
    }

    void ResolveCarSpawn()
    {
        if (carSpawn != null)
        {
            return;
        }
        var t = transform.Find("CarSpawn");
        if (t != null)
        {
            carSpawn = t;
        }
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

    /// <summary>本关期望的正交视野半高（世界单位），对应 <see cref="Camera.orthographicSize"/>。</summary>
    public float LevelOrthographicSize => levelOrthographicSize;

    /// <summary>本关总圈数（1~5），由 GameMode 推给 Session 用于 HUD 初始化与计时。</summary>
    public int LapCount => Mathf.Clamp(lapCount, 1, 5);

    public RcCarFinishLine2D[] GetFinishLinesInLevel()
    {
        return GetComponentsInChildren<RcCarFinishLine2D>(true);
    }

    /// <summary>关卡里所有反作弊中段检查点（无序）；用于 Session 圈完成守卫。</summary>
    public RcCarMidCheckpoint2D[] GetMidCheckpointsInLevel()
    {
        return GetComponentsInChildren<RcCarMidCheckpoint2D>(true);
    }
}
