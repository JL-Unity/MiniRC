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
}
