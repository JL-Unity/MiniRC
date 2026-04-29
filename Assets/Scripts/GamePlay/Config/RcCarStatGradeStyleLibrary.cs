using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 车辆属性等级展示样式 + 阈值。
/// 颜色统一由 <see cref="UiColorPalette"/> 提供，本 SO 仅持 token；token == None 时打 warning 并返回 fallback。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/RC Car Stat Grade Style Library", fileName = "RcCarStatGradeStyleLibrary")]
public class RcCarStatGradeStyleLibrary : ScriptableObject
{
    [Serializable]
    struct GradeStyle
    {
        public UiColorToken textColorToken;
        [Tooltip("达到此 percent 即归为本等级；从高到低判定。C 为兜底等级，其字段不参与判定。")]
        [Range(0, 100)] public int minPercent;
    }

    // 默认值采用非线性阈值（S+ 较稀有）。新建 SO 时直接生效；已有资产可在 Inspector 右键 Reset 恢复。
    [SerializeField] GradeStyle c = new GradeStyle { minPercent = 0 };
    [SerializeField] GradeStyle cPlus = new GradeStyle { minPercent = 20 };
    [SerializeField] GradeStyle b = new GradeStyle { minPercent = 35 };
    [SerializeField] GradeStyle bPlus = new GradeStyle { minPercent = 50 };
    [SerializeField] GradeStyle a = new GradeStyle { minPercent = 60 };
    [SerializeField] GradeStyle aPlus = new GradeStyle { minPercent = 75 };
    [SerializeField] GradeStyle s = new GradeStyle { minPercent = 85 };
    [SerializeField] GradeStyle sPlus = new GradeStyle { minPercent = 95 };

    HashSet<int> _warnedTokenNone;

    /// <summary>按配置阈值把 percent（0–100）映射到等级；从高到低查，第一个满足的胜出。</summary>
    public RcCarStatGrade FromPercent(int percent)
    {
        int p = Mathf.Clamp(percent, 0, 100);
        if (p >= sPlus.minPercent) return RcCarStatGrade.SPlus;
        if (p >= s.minPercent) return RcCarStatGrade.S;
        if (p >= aPlus.minPercent) return RcCarStatGrade.APlus;
        if (p >= a.minPercent) return RcCarStatGrade.A;
        if (p >= bPlus.minPercent) return RcCarStatGrade.BPlus;
        if (p >= b.minPercent) return RcCarStatGrade.B;
        if (p >= cPlus.minPercent) return RcCarStatGrade.CPlus;
        return RcCarStatGrade.C;
    }

    /// <summary>
    /// 取等级对应的字体颜色；<see cref="RcCarStatGrade.None"/> 时返回 <paramref name="fallback"/>。
    /// </summary>
    public Color GetTextColor(RcCarStatGrade grade, Color fallback)
    {
        switch (grade)
        {
            case RcCarStatGrade.C: return ResolveColor(grade, c, fallback);
            case RcCarStatGrade.CPlus: return ResolveColor(grade, cPlus, fallback);
            case RcCarStatGrade.B: return ResolveColor(grade, b, fallback);
            case RcCarStatGrade.BPlus: return ResolveColor(grade, bPlus, fallback);
            case RcCarStatGrade.A: return ResolveColor(grade, a, fallback);
            case RcCarStatGrade.APlus: return ResolveColor(grade, aPlus, fallback);
            case RcCarStatGrade.S: return ResolveColor(grade, s, fallback);
            case RcCarStatGrade.SPlus: return ResolveColor(grade, sPlus, fallback);
            default: return fallback;
        }
    }

    Color ResolveColor(RcCarStatGrade grade, GradeStyle e, Color fallback)
    {
        if (e.textColorToken == UiColorToken.None)
        {
            WarnTokenNoneOnce(grade);
            return fallback;
        }
        return UiColorService.GetInstance().Get(e.textColorToken, fallback);
    }

    void WarnTokenNoneOnce(RcCarStatGrade grade)
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

#if UNITY_EDITOR
    void OnValidate()
    {
        // C 不参与判定，从 C+ 开始检查阈值是否单调递增；不强制纠正，只在 Console 给提醒。
        int prev = cPlus.minPercent;
        string prevName = "C+";
        CheckMonotonic(ref prev, ref prevName, b.minPercent, "B");
        CheckMonotonic(ref prev, ref prevName, bPlus.minPercent, "B+");
        CheckMonotonic(ref prev, ref prevName, a.minPercent, "A");
        CheckMonotonic(ref prev, ref prevName, aPlus.minPercent, "A+");
        CheckMonotonic(ref prev, ref prevName, s.minPercent, "S");
        CheckMonotonic(ref prev, ref prevName, sPlus.minPercent, "S+");
    }

    void CheckMonotonic(ref int prev, ref string prevName, int cur, string curName)
    {
        if (cur < prev)
        {
            Debug.LogWarning(
                $"{name}: 阈值非单调递增 — {curName}.minPercent ({cur}) < {prevName}.minPercent ({prev})。" +
                $"判定结果可能不符合预期。", this);
        }
        prev = cur;
        prevName = curName;
    }
#endif
}
