/// <summary>
/// Start 菜单栈内弹出面板的动画约定：<see cref="PlayOpenAnimation"/> 在进入栈顶（<c>OnEnter</c>）时调用；
/// <see cref="PlayCloseAnimation"/> 在返回/关闭时触发关闭剪辑；剪辑末尾请添加 Animation Event 指向
/// <see cref="OnCloseAnimationComplete"/>（或与无 <c>ClosePanel</c> 剪辑时由 <see cref="PlayCloseAnimation"/> 直接调用完成逻辑）。
/// Legacy <see cref="UnityEngine.Animation"/> 剪辑需勾选 Legacy，且名称与预制体列表一致（默认 OpenPanel / ClosePanel）。
/// </summary>
public interface IStartMenuPanelAnimation
{
    /// <summary>打开面板动画（对应剪辑名通常为 OpenPanel）。</summary>
    void PlayOpenAnimation();

    /// <summary>播放关闭动画；若无 ClosePanel 剪辑则通常会直接 <see cref="OnCloseAnimationComplete"/>。</summary>
    void PlayCloseAnimation();

    /// <summary>关闭动画播放完毕：弹栈。由关闭剪辑末尾的 Animation Event 调用。</summary>
    void OnCloseAnimationComplete();
}
