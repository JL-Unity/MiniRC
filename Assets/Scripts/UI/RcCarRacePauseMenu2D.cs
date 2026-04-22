using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单：只负责把按钮接到 <see cref="RcCarRaceGameMode"/>，逻辑在 GameMode。
/// 由 GameMode 在 <c>OnStart</c> 中调用 <see cref="Bind"/>（暂停根节点初始可能未激活，需用代码绑定）。
/// </summary>
[DisallowMultipleComponent]
public class RcCarRacePauseMenu2D : MonoBehaviour
{
    [SerializeField] Button continueButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button exitButton;

    RcCarRaceGameMode _mode;
    bool _bound;

    public void Bind(RcCarRaceGameMode mode)
    {
        if (_bound || mode == null)
        {
            return;
        }
        _bound = true;
        _mode = mode;
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

    void OnDestroy()
    {
        if (!_bound)
        {
            return;
        }
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

    void OnContinueClicked() => _mode?.ResumeRace();

    void OnRestartClicked() => _mode?.RestartRaceFromPauseMenu();

    void OnExitClicked() => _mode?.ExitRace();
}
