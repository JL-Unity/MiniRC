/// <summary>
/// 计时赛成绩等级。<see cref="None"/> 仅表示「无成绩 / 配置缺失」，UI 上不应显示等级图标；
/// 任何已完成的成绩至少给到 <see cref="C"/>。
/// </summary>
public enum RcRaceGrade
{
    None = 0,
    C,
    B,
    A,
    S,
    SS,
}
