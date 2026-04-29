using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 等级 → 展示样式（图标 + 字体颜色）的集中映射 SO。
/// 颜色统一由 <see cref="UiColorPalette"/> 提供，本 SO 仅持 token；token == None 时打 warning 并返回 fallback。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/RC Race Grade Style Library", fileName = "RcRaceGradeStyleLibrary")]
public class RcRaceGradeStyleLibrary : ScriptableObject
{
    [Serializable]
    struct GradeStyle
    {
        public Sprite sprite;
        public UiColorToken textColorToken;
    }

    [SerializeField] GradeStyle c;
    [SerializeField] GradeStyle b;
    [SerializeField] GradeStyle a;
    [SerializeField] GradeStyle s;
    [SerializeField] GradeStyle ss;

    // 节流：每个等级 token == None 的 warning 仅打一次，避免 Editor 内每帧调用刷屏。
    HashSet<int> _warnedTokenNone;

    public Sprite GetSprite(RcRaceGrade grade)
    {
        switch (grade)
        {
            case RcRaceGrade.C: return c.sprite;
            case RcRaceGrade.B: return b.sprite;
            case RcRaceGrade.A: return a.sprite;
            case RcRaceGrade.S: return s.sprite;
            case RcRaceGrade.SS: return ss.sprite;
            default: return null;
        }
    }

    /// <summary>
    /// 取等级对应的字体颜色；<see cref="RcRaceGrade.None"/> 或未识别枚举时返回 <paramref name="fallback"/>。
    /// </summary>
    public Color GetTextColor(RcRaceGrade grade, Color fallback)
    {
        switch (grade)
        {
            case RcRaceGrade.C: return ResolveColor(grade, c, fallback);
            case RcRaceGrade.B: return ResolveColor(grade, b, fallback);
            case RcRaceGrade.A: return ResolveColor(grade, a, fallback);
            case RcRaceGrade.S: return ResolveColor(grade, s, fallback);
            case RcRaceGrade.SS: return ResolveColor(grade, ss, fallback);
            default: return fallback;
        }
    }

    Color ResolveColor(RcRaceGrade grade, GradeStyle e, Color fallback)
    {
        if (e.textColorToken == UiColorToken.None)
        {
            WarnTokenNoneOnce(grade);
            return fallback;
        }
        return UiColorService.GetInstance().Get(e.textColorToken, fallback);
    }

    void WarnTokenNoneOnce(RcRaceGrade grade)
    {
        if (_warnedTokenNone == null)
        {
            _warnedTokenNone = new HashSet<int>();
        }
        if (_warnedTokenNone.Add((int)grade))
        {
            LogClass.LogWarning(GameLogCategory.UIManager,
                $"{name}: 等级 {grade} 的 textColorToken 未配置（None），返回 fallback");
        }
    }
}
