using System;
using UnityEngine;

/// <summary>
/// 关卡 id → 关卡根预制体（内含 <see cref="RcRaceLevelRoot"/> 与 CarSpawn）。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/RC Track Catalog", fileName = "RcTrackCatalog")]
public class RcTrackCatalog : ScriptableObject
{
    [Serializable]
    public class TrackEntry
    {
        public string levelId;
        [Tooltip("选关面板按钮上显示的名称；留空则用 levelId")]
        public string displayName;
        public GameObject levelPrefab;
        [Tooltip("选关面板预览图；为空则隐藏预览 Image")]
        public Sprite previewSprite;

        [Header("等级阈值（达成该等级所需的最高秒数；从 SS 到 C 依次递增）")]
        [Tooltip("≤ 此秒数判定为 SS")]
        public float gradeSsMaxSeconds = 30f;
        [Tooltip("≤ 此秒数判定为 S（必须 > SS 阈值）")]
        public float gradeSMaxSeconds = 35f;
        [Tooltip("≤ 此秒数判定为 A（必须 > S 阈值）")]
        public float gradeAMaxSeconds = 40f;
        [Tooltip("≤ 此秒数判定为 B（必须 > A 阈值）")]
        public float gradeBMaxSeconds = 50f;
    }

    public TrackEntry[] tracks;

    public GameObject GetLevelPrefab(string levelId)
    {
        if (tracks == null)
        {
            return null;
        }
        foreach (var e in tracks)
        {
            if (e != null && e.levelPrefab != null && e.levelId == levelId)
            {
                return e.levelPrefab;
            }
        }

        return null;
    }

    /// <summary>按 levelId 查 entry。不要求 levelPrefab 非空（区别于 <see cref="GetLevelPrefab"/>），
    /// 给只需要元数据（displayName / previewSprite）的 UI 用。</summary>
    public TrackEntry GetEntry(string levelId)
    {
        if (tracks == null || string.IsNullOrEmpty(levelId))
        {
            return null;
        }
        foreach (var e in tracks)
        {
            if (e != null && e.levelId == levelId)
            {
                return e;
            }
        }
        return null;
    }

    /// <summary>
    /// 根据完赛秒数判定等级。entry 缺失或秒数非有效成绩（<=0 / NaN）返回 <see cref="RcRaceGrade.None"/>，
    /// 完成即至少给 <see cref="RcRaceGrade.C"/>，依次往 SS 收紧。
    /// </summary>
    public RcRaceGrade EvaluateGrade(string levelId, float seconds)
    {
        // 容错 PlayerPrefs 缺省值（float.MaxValue 表示"未通关"）以及无效值
        if (float.IsNaN(seconds) || seconds <= 0f || seconds >= float.MaxValue - 1f)
        {
            return RcRaceGrade.None;
        }

        var entry = GetEntry(levelId);
        if (entry == null)
        {
            return RcRaceGrade.None;
        }

        if (seconds <= entry.gradeSsMaxSeconds) return RcRaceGrade.SS;
        if (seconds <= entry.gradeSMaxSeconds)  return RcRaceGrade.S;
        if (seconds <= entry.gradeAMaxSeconds)  return RcRaceGrade.A;
        if (seconds <= entry.gradeBMaxSeconds)  return RcRaceGrade.B;
        // 比 B 还慢的所有完成成绩兜底为 C
        return RcRaceGrade.C;
    }
}
