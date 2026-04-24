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
    }

    public override void OnPause() { }

    public override void OnResume() { }

    void OnContinueClicked() => Mode?.ResumeRace();

    void OnRestartClicked() => Mode?.RestartRaceFromPauseMenu();

    void OnExitClicked() => Mode?.ExitRace();
}
