/// <summary>
/// 车辆单项属性（speed/accel/grip 等 0–100 的 percent）的等级。
/// 与赛事评级 <see cref="RcRaceGrade"/> 解耦，分级与阈值独立维护；
/// 阈值由 <see cref="RcCarStatGradeStyleLibrary.FromPercent"/> 配置驱动，本文件不持有任何硬编码阈值。
/// </summary>
public enum RcCarStatGrade
{
    None,
    C,
    CPlus,
    B,
    BPlus,
    A,
    APlus,
    S,
    SPlus,
}

public static class RcCarStatGradeExtensions
{
    /// <summary>UI 显示文本，例如 "C+" / "S+"；<see cref="RcCarStatGrade.None"/> 返回空串。</summary>
    public static string ToDisplay(this RcCarStatGrade g)
    {
        switch (g)
        {
            case RcCarStatGrade.C: return "C";
            case RcCarStatGrade.CPlus: return "C+";
            case RcCarStatGrade.B: return "B";
            case RcCarStatGrade.BPlus: return "B+";
            case RcCarStatGrade.A: return "A";
            case RcCarStatGrade.APlus: return "A+";
            case RcCarStatGrade.S: return "S";
            case RcCarStatGrade.SPlus: return "S+";
            default: return "";
        }
    }
}
