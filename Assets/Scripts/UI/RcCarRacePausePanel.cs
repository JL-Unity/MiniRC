using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停面板：由 <see cref="RcCarRaceGameMode.TryPauseRace"/> 走 UIManager.PushPanel 推入；
/// 按钮只转发到 GameMode 公开方法，不直接动 timeScale / Session。
/// </summary>
public class RcCarRacePausePanel : BasePanel
{
    [SerializeField] Button continueButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button exitButton;
    [SerializeField] Button settingsButton;

    RcCarRaceGameMode Mode => GameManager.Instance?.GetGameMode() as RcCarRaceGameMode;

    public override void OnEnter()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
        }
        if(settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
    }

    public override void OnExit()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitClicked);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }
    }

    public override void OnPause() { }

    public override void OnResume() { }

    void OnContinueClicked()
    {
        AudioManager.GetInstance().PlayUiClose();
        Mode?.ResumeRace();
    }

    void OnRestartClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        Mode?.RestartRaceFromPauseMenu();
    }

    void OnExitClicked()
    {
        AudioManager.GetInstance().PlayUiClose();
        Mode?.ExitRace();
    }

    void OnSettingsClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        UIManager.GetInstance().PushPanel(PanelPath.SettingsPanelName);
    }
}
