using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开发者名单面板：文本来源由预制体 Inspector 直接手填（<see cref="creditsText"/>）。
/// 返回按钮先 <see cref="PlayCloseAnimation"/>，片尾 Event 再 <see cref="OnCloseAnimationComplete"/>。
/// </summary>
public class RcCreditsPanel : BasePanel, IStartMenuPanelAnimation
{
    [SerializeField] Button backButton;

    void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
    }

    public override void OnEnter()
    {
        PlayOpenAnimation();
    }

    public void PlayOpenAnimation()
    {
        PanelAnimationUtil.TryPlayClip(GetComponent<Animation>(), PanelAnimationUtil.DefaultOpenClipName);
    }

    public void PlayCloseAnimation()
    {
        if (!PanelAnimationUtil.TryPlayClip(GetComponent<Animation>(), PanelAnimationUtil.DefaultCloseClipName))
        {
            OnCloseAnimationComplete();
        }
    }

    public void OnCloseAnimationComplete()
    {
        UIManager.GetInstance().PopPanel();
    }

    public override void OnPause() { }
    public override void OnResume() { }
    public override void OnExit() { }

    void OnBackClicked()
    {
        AudioManager.GetInstance().PlayUiClose();
        PlayCloseAnimation();
    }
}
