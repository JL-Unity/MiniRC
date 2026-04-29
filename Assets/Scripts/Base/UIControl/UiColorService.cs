using UnityEngine;

/// <summary>
/// 全局 UI 颜色服务：业务 SO / Panel 不直接持 Palette 引用，统一通过 <see cref="GetInstance"/> 查询。
/// 启动时由 <see cref="GameManager.Init"/> 调用 <see cref="Init"/>，从 Resources 加载默认 <see cref="UiColorPalette"/>。
/// </summary>
public class UiColorService : BaseManager<UiColorService>
{
    /// <summary>默认色板资产名（必须放在某个 Resources 目录下，且文件名一致）。</summary>
    public const string DefaultPaletteResourcePath = "UiColorPalette";

    UiColorPalette _palette;

    public override void Init()
    {
        if (_palette == null)
        {
            _palette = Resources.Load<UiColorPalette>(DefaultPaletteResourcePath);
        }

        if (_palette == null)
        {
            LogClass.LogWarning(GameLogCategory.System,
                $"UiColorService: 未能从 Resources 加载默认色板 '{DefaultPaletteResourcePath}'，所有 token 查询将返回 fallback");
        }
    }

    /// <summary>运行时手动注入 Palette（测试 / 多色板切换场景），覆盖 Init 加载的默认值。</summary>
    public void SetPalette(UiColorPalette palette)
    {
        _palette = palette;
    }

    /// <summary>按 token 取颜色；色板未加载或 token 未配置时返回 <paramref name="fallback"/>。</summary>
    public Color Get(UiColorToken token, Color fallback)
    {
        if (_palette == null)
        {
            return fallback;
        }
        return _palette.Get(token, fallback);
    }
}
