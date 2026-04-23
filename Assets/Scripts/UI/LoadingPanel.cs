using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开发者名单面板：文本来源由预制体 Inspector 直接手填（<see cref="creditsText"/>）。
/// 返回按钮先 <see cref="PlayCloseAnimation"/>，片尾 Event 再 <see cref="OnCloseAnimationComplete"/>。
/// </summary>
public class LoadingPanel : BasePanel
{
    [SerializeField] Slider loadingSlider;
    

    public void SetLoadingProgress(float progress)
    {
        loadingSlider.value = progress;
    }

    public override void OnEnter()
    {
        LogClass.LogGame(GameLogCategory.System, "Game loading");
    }

    public override void OnPause() { }

    public override void OnResume() { }

    public override void OnExit() { }
}
