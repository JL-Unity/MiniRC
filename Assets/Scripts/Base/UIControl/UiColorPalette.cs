using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局 UI 颜色色板 SO；以 <see cref="UiColorToken"/> 为 key 集中维护项目所有「语义颜色」。
/// 业务方通过 <see cref="UiColorService"/> 查询，避免在各 SO 里散落硬编码 Color。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/UI Color Palette", fileName = "UiColorPalette")]
public class UiColorPalette : ScriptableObject
{
    [Serializable]
    struct Entry
    {
        public UiColorToken token;
        public Color color;
    }

    [SerializeField] List<Entry> entries = new List<Entry>();

    Dictionary<UiColorToken, Color> _map;

    /// <summary>按 token 查颜色；token 为 <see cref="UiColorToken.None"/> 或未配置时返回 <paramref name="fallback"/>。</summary>
    public Color Get(UiColorToken token, Color fallback)
    {
        if (token == UiColorToken.None)
        {
            return fallback;
        }
        EnsureMap();
        return _map.TryGetValue(token, out var c) ? c : fallback;
    }

    void EnsureMap()
    {
        if (_map != null)
        {
            return;
        }
        _map = new Dictionary<UiColorToken, Color>(entries.Count);
        foreach (var e in entries)
        {
            // 同一 token 配多次时后写覆盖前；OnValidate 已给 warning，运行时不报错保持鲁棒
            _map[e.token] = e.color;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // entries 在 Editor 修改后清空缓存；下次 Get 时重建
        _map = null;

        var seen = new HashSet<UiColorToken>();
        foreach (var e in entries)
        {
            if (!seen.Add(e.token))
            {
                Debug.LogWarning($"{name}: 重复 token {e.token}，后写覆盖前。", this);
            }
        }
    }
#endif
}
