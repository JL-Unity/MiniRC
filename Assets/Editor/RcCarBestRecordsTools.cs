#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// -----------------------------------------------------------------------------
// 编辑器小工具：清空 RC 计时赛的最佳成绩 PlayerPrefs。
// · 键名规则与 RcCarRaceGameMode 保持一致：MiniRC_RcRace_BestTotal_{levelId}
// · 扫描项目里所有 RcTrackCatalog 资产，按 entry.levelId 列表精准删除，
//   避免 PlayerPrefs.DeleteAll 把音量等设置一起清掉
// · 兜底再尝试删一个 levelId="001"（GameMode 的默认值，万一未走 catalog 也能命中）
// · 仅在 UNITY_EDITOR 下编译，不进打包
// -----------------------------------------------------------------------------

public static class RcCarBestRecordsTools
{
    const string MenuPath = "Tools/Mini RC/Clear All Best Records";
    const string KeyPrefix = "MiniRC_RcRace_BestTotal_";
    const string FallbackLevelId = "001";

    [MenuItem(MenuPath)]
    public static void ClearAllBestRecords()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear Best Records",
                "Delete all stored best lap records (PlayerPrefs)? This cannot be undone.",
                "Yes, clear", "Cancel"))
        {
            return;
        }

        var keysToDelete = new HashSet<string>();

        // 从所有 RcTrackCatalog 资产里收集 levelId
        string[] guids = AssetDatabase.FindAssets("t:RcTrackCatalog");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var catalog = AssetDatabase.LoadAssetAtPath<RcTrackCatalog>(path);
            if (catalog == null || catalog.tracks == null)
            {
                continue;
            }
            foreach (var entry in catalog.tracks)
            {
                if (entry == null || string.IsNullOrEmpty(entry.levelId))
                {
                    continue;
                }
                keysToDelete.Add(KeyPrefix + entry.levelId);
            }
        }

        // 兜底：默认 trackId（场景未通过菜单选关时 RcCarRaceGameMode 用的就是这个）
        keysToDelete.Add(KeyPrefix + FallbackLevelId);

        int removed = 0;
        foreach (var key in keysToDelete)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                Debug.Log($"[RcCarBestRecordsTools] Removed PlayerPrefs key: {key}");
                removed++;
            }
        }

        PlayerPrefs.Save();

        EditorUtility.DisplayDialog(
            "Clear Best Records",
            removed > 0
                ? $"Cleared {removed} best record(s)."
                : "No best record found (already clean).",
            "OK");
    }
}
#endif
