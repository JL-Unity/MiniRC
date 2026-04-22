using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开发者名单面板：文本来源由预制体 Inspector 直接手填（<see cref="creditsText"/>）。
/// 返回按钮走 UIManager 栈弹回上一层。
/// </summary>
public class RcCreditsPanel : BasePanel
{    [SerializeField] Button backButton;

    void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
    }

    public override void OnEnter() {}

    public override void OnPause() { }
    public override void OnResume() { }
    public override void OnExit() { }

    void OnBackClicked()
    {
        UIManager.GetInstance().PopPanel();
    }
}
