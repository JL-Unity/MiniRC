using UnityEngine;

/// <summary>
/// 单辆可选 RC 车：展示名、预制体、三条属性（0–100，对应 UI 条与整数显示）。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/RC Car Definition", fileName = "RcCarDefinition")]
public class RcCarDefinition : ScriptableObject
{
    [Tooltip("逻辑 id，可选")]
    public string carId;

    public string displayName = "RC";

    public GameObject carPrefab;

    [Range(0, 100)] public int speedPercent = 50;
    [Range(0, 100)] public int driftPercent = 50;
    [Range(0, 100)] public int accelPercent = 50;
}
