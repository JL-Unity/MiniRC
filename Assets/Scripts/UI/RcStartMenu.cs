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
    [SerializeField] Button instructionsButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;

    [Header("Resources 下的面板资源名（不带扩展名）")]
    [SerializeField] string levelSelectPanelName = "RcLevelSelectPanel";
    [SerializeField] string creditsPanelName = "RcCreditsPanel";
    [SerializeField] string highScorePanelName = "RcHighScorePanel";
    [SerializeField] string instructionsPanelName = "RcInstructionsPanel";
    [SerializeField] string settingsPanelName = "RcSettingsPanel";

    void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
        if (highScoreButton != null) highScoreButton.onClick.AddListener(OnHighScoreClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (instructionsButton != null) instructionsButton.onClick.AddListener(OnInstructionsClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
        if (highScoreButton != null) highScoreButton.onClick.RemoveListener(OnHighScoreClicked);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (instructionsButton != null) instructionsButton.onClick.RemoveListener(OnInstructionsClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    void OnSettingsClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        PushStartUpPanel(settingsPanelName);
    }

    void OnInstructionsClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        PushStartUpPanel(instructionsPanelName);
    }

    void OnStartClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        PushStartUpPanel(levelSelectPanelName);
    }

    void OnCreditsClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        PushStartUpPanel(creditsPanelName);
    }

    void OnHighScoreClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        PushStartUpPanel(highScorePanelName);
    }

    void PushStartUpPanel(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return;
        UIManager.GetInstance().PushPanel(PanelPath.StartUpPath + resourceName);
    }

    void OnQuitClicked()
    {
        AudioManager.GetInstance().PlayUiClose();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
