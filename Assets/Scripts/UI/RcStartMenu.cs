using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartScene 主菜单入口：作为场景里的普通 MonoBehaviour 挂在 Canvas 下，
/// 四个按钮分别驱动「开始（选关）/ 开发者名单 / 最高记录 / 退出」。
/// 子面板（选关 / 名单 / 记录）统一走 UIManager 栈，要求 prefab 放在
/// Assets/Resources/UI/StartUp/ 下，文件名即此处拼出的资源名。
/// </summary>
public class RcStartMenu : MonoBehaviour
{
    [Header("菜单按钮")]
    [SerializeField] Button startButton;
    [SerializeField] Button creditsButton;
    [SerializeField] Button highScoreButton;
    [SerializeField] Button quitButton;

    [Header("Resources 下的面板资源名（不带扩展名）")]
    [SerializeField] string levelSelectPanelName = "RcLevelSelectPanel";
    [SerializeField] string creditsPanelName = "RcCreditsPanel";
    [SerializeField] string highScorePanelName = "RcHighScorePanel";

    void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
        if (highScoreButton != null) highScoreButton.onClick.AddListener(OnHighScoreClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
        if (highScoreButton != null) highScoreButton.onClick.RemoveListener(OnHighScoreClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    void OnStartClicked()
    {
        PushStartUpPanel(levelSelectPanelName);
    }

    void OnCreditsClicked()
    {
        PushStartUpPanel(creditsPanelName);
    }

    void OnHighScoreClicked()
    {
        PushStartUpPanel(highScorePanelName);
    }

    void PushStartUpPanel(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return;
        UIManager.GetInstance().PushPanel(PanelPath.StartUpPath + resourceName);
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
