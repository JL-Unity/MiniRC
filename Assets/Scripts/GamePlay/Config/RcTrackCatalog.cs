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
        public GameObject levelPrefab;
    }

    public TrackEntry[] tracks;

    public GameObject GetLevelPrefab(string levelId)
    {
        if (tracks == null)
            return null;
        foreach (var e in tracks)
        {
            if (e != null && e.levelPrefab != null && e.levelId == levelId)
                return e.levelPrefab;
        }

        return null;
    }
}
