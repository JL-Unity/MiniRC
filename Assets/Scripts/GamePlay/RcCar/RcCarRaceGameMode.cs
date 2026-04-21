using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// RC 计时赛场景用 GameMode：暂停/继续、暂停内重开与退出、结算「再来一局」等入口统一在此。
/// </summary>
[DisallowMultipleComponent]
public class RcCarRaceGameMode : GameMode
{
    const string PlayerPrefsBestKeyPrefix = "MiniRC_RcRace_BestTotal_";

    [SerializeField] RcCarRaceSession2D raceSession;
    [SerializeField] GameObject pauseMenuRoot;
    [SerializeField] Button resultPlayAgainButton;
    [Tooltip("退出时加载的场景名；留空则仅 Application.Quit（编辑器下可能无效果）")]
    [SerializeField] string exitSceneName = "";
    [SerializeField] string trackId = "Default";

    string BestPrefsKey => PlayerPrefsBestKeyPrefix + trackId;

    /// <summary>
    /// 用本轮总成绩更新本地最佳（PlayerPrefs）。<paramref name="bestShownSeconds"/> 为结果面板上应显示的「最好成绩」秒数。
    /// </summary>
    public void ResolveBestTotal(float totalSeconds, out float bestShownSeconds, out bool newRecord)
    {
        totalSeconds = RcCarRaceSession2D.RoundToHundredths(totalSeconds);
        float rawBest = PlayerPrefs.GetFloat(BestPrefsKey, float.MaxValue);
        float prevBestRounded = rawBest < float.MaxValue - 1f
            ? RcCarRaceSession2D.RoundToHundredths(rawBest)
            : rawBest;
        newRecord = totalSeconds < prevBestRounded - 0.0005f;
        if (newRecord)
            PlayerPrefs.SetFloat(BestPrefsKey, totalSeconds);
        bestShownSeconds = newRecord ? totalSeconds : prevBestRounded;
    }

    bool _pausedFromRace;

    protected override void OnStart()
    {
        base.OnStart();
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
            var pauseUi = pauseMenuRoot.GetComponent<RcCarRacePauseMenu2D>()
                ?? pauseMenuRoot.GetComponentInChildren<RcCarRacePauseMenu2D>(true);
            pauseUi?.Bind(this);
        }

        if (resultPlayAgainButton != null)
            resultPlayAgainButton.onClick.AddListener(PlayAgainFromResult);
    }

    /// <summary>结算面板上「再来一局」：仅复位 Session，不涉及暂停菜单。</summary>
    public void PlayAgainFromResult()
    {
        raceSession?.ResetRaceToWaitingAtSpawn();
    }

    /// <summary>暂停菜单内重新开始：结束暂停并回到等待首帧输入。</summary>
    public void RestartRaceFromPauseMenu()
    {
        if (!_pausedFromRace)
            return;

        _pausedFromRace = false;
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        ResumeGame();
        raceSession?.ResetRaceToWaitingAtSpawn();
    }

    /// <summary>暂停菜单内退出：解除暂停后加载 <see cref="exitSceneName"/> 或退出应用。</summary>
    public void ExitRace()
    {
        if (_pausedFromRace)
        {
            _pausedFromRace = false;
            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);
            ResumeGame();
        }

        if (!string.IsNullOrEmpty(exitSceneName))
            SceneManager.LoadScene(exitSceneName);
        else
            Application.Quit();
    }

    /// <summary>仅在比赛中生效；已暂停时重复调用无效。</summary>
    public void TryPauseRace()
    {
        if (_pausedFromRace)
            return;
        if (raceSession == null || !raceSession.IsRacing)
            return;

        _pausedFromRace = true;
        StopGame();
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);
    }

    /// <summary>关闭暂停菜单并恢复 timeScale；非本模式触发的暂停不会执行任何操作。</summary>
    public void ResumeRace()
    {
        if (!_pausedFromRace)
            return;

        _pausedFromRace = false;
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        ResumeGame();
    }

    protected override void GameStart() { }

    protected override void GameEnd(GameEndReason reason) { }

    protected override bool CheckGameReady() => true;

    protected override void M_OnDestroy()
    {
        if (resultPlayAgainButton != null)
            resultPlayAgainButton.onClick.RemoveListener(PlayAgainFromResult);
        if (_pausedFromRace)
            ResumeRace();
        base.M_OnDestroy();
    }
}
