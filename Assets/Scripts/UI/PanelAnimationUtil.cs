using UnityEngine;

/// <summary>
/// Legacy <see cref="Animation"/> 播放辅助：<see cref="Animation.Play(string)"/> 只能播放
/// 已挂在本组件 <c>Animations</c> 列表里的 Clip，且字符串必须与 <see cref="AnimationClip.name"/> 一致。
/// 注意：剪辑在 Project 里必须勾选 Legacy（Animator 用的 Generic 剪辑 <c>m_Legacy: 0</c> 不会出现在 <see cref="Animation.GetClip"/> 里）。
/// </summary>
public static class PanelAnimationUtil
{
    public const string DefaultOpenClipName = "OpenPanel";
    public const string DefaultCloseClipName = "ClosePanel";

    /// <summary>若存在对应 Clip 则播放，否则仅打日志（避免 Unity 默认报错刷屏）。</summary>
    public static bool TryPlayClip(Animation animation, string clipName)
    {
        if (animation == null || string.IsNullOrEmpty(clipName))
        {
            return false;
        }

        // GetClip 仅能看到已拖入 Animation 组件的剪辑；缺依赖时会返回 null
        if (animation.GetClip(clipName) == null)
        {
            LogClass.LogWarning(
                GameLogCategory.UIManager,
                $"{animation.gameObject.name}: 未找到名为「{clipName}」的 AnimationClip。" +
                "请在该物体 Legacy Animation 组件的列表中加入对应 .anim，并确认 clip.name 与字符串一致。");
            return false;
        }

        animation.Play(clipName);
        return true;
    }
}
