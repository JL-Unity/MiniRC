/// <summary>
/// 全局 UI 语义颜色 key；由 <see cref="UiColorPalette"/> 配色，<see cref="UiColorService"/> 提供查询。
/// </summary>
/// <remarks>
/// 重要：<b>绝不要在中间插入新枚举项或重新排列</b>，否则 Inspector 已配的 token 字段会随枚举值偏移到错误项。
/// 新增 token 一律追加到 enum 末尾。已废弃的 token 保留位置，必要时改名加 _Deprecated 后缀。
/// </remarks>
public enum UiColorToken
{
    None = 0,

    // 车属性等级（RcCarStatGrade）
    Green,
    GreenPlus,
    Blue,
    BluePlus,
    Purple,
    PurplePlus,
    Orange,
    OrangePlus,
    Red,
    RedPlus,
}
