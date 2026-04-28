using System;
using UnityEngine;

/// <summary>
/// 等级 → 展示样式（图标 + 字体颜色，未来可扩描边/字号等）的集中映射 SO。
/// 所有面板上的等级图标走 <see cref="RcRaceGradeIconView"/>，等级配色由调用方
/// 直接 <see cref="GetTextColor"/> 取并设到自家 Text/TMP，避免到处写硬编码颜色。
/// <see cref="RcRaceGrade.None"/> 时 <see cref="GetSprite"/> 返回 null（视图据此隐藏）；
/// <see cref="GetTextColor"/> 返回 fallback 让调用方决定回退色。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/RC Race Grade Style Library", fileName = "RcRaceGradeStyleLibrary")]
public class RcRaceGradeStyleLibrary : ScriptableObject
{
    [Serializable]
    struct GradeStyle
    {
        public Sprite sprite;
        public Color textColor;
    }

    // 沿用枚举顺序但拆成独立字段，Inspector 一目了然；新增样式属性时只在结构体里加一字段
    [SerializeField] GradeStyle c = new GradeStyle { textColor = Color.white };
    [SerializeField] GradeStyle b = new GradeStyle { textColor = Color.white };
    [SerializeField] GradeStyle a = new GradeStyle { textColor = Color.white };
    [SerializeField] GradeStyle s = new GradeStyle { textColor = Color.white };
    [SerializeField] GradeStyle ss = new GradeStyle { textColor = Color.white };

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
    /// 取等级对应的字体颜色；<see cref="RcRaceGrade.None"/> 或未识别枚举时返回 <paramref name="fallback"/>，
    /// 让调用端能够保留原色（典型用法：<c>text.color = lib.GetTextColor(grade, text.color);</c>）。
    /// </summary>
    public Color GetTextColor(RcRaceGrade grade, Color fallback)
    {
        switch (grade)
        {
            case RcRaceGrade.C: return c.textColor;
            case RcRaceGrade.B: return b.textColor;
            case RcRaceGrade.A: return a.textColor;
            case RcRaceGrade.S: return s.textColor;
            case RcRaceGrade.SS: return ss.textColor;
            default: return fallback;
        }
    }
}
