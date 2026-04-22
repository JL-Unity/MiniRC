using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// RC 计时赛 GameMode：暂停/结算/再来一局；Race 场景内按 GameManager 选关选车实例化关卡与车辆。
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

    [Header("关卡 / 车辆（菜单写入 GameManager，进入 Race 后实例化；留空则沿用场景中已拖引用）")]
    [SerializeField] RcTrackCatalog trackCatalog;
    [SerializeField] RcCarRoster carRoster;
    [SerializeField] Transform levelAnchor;
    [Tooltip("可选：与车预制体内 Controller 一致的 UI 摇杆")]
    [SerializeField] Joystick uiJoystick;

    GameObject _levelInstance;

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
        {
            PlayerPrefs.SetFloat(BestPrefsKey, totalSeconds);
        }
        bestShownSeconds = newRecord ? totalSeconds : prevBestRounded;
    }

    bool _pausedFromRace;

    protected override void OnStart()
    {
        base.OnStart();
        TryInitializeRaceFromGameManager();

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
            var pauseUi = pauseMenuRoot.GetComponent<RcCarRacePauseMenu2D>()
                ?? pauseMenuRoot.GetComponentInChildren<RcCarRacePauseMenu2D>(true);
            pauseUi?.Bind(this);
        }

        if (resultPlayAgainButton != null)
        {
            resultPlayAgainButton.onClick.AddListener(PlayAgainFromResult);
        }
    }

    /// <summary>
    /// 菜单选关/选车后进入本场景：按目录生成关卡预制体与车辆并绑定 Session。
    /// 若未配置 catalog/roster/anchor，则保留场景中已序列化的车与 Session（兼容旧场景）。
    /// </summary>
    void TryInitializeRaceFromGameManager()
    {
        if (raceSession == null || GameManager.Instance == null)
        {
            return;
        }
        if (trackCatalog == null || carRoster == null || levelAnchor == null)
        {
            return;
        }

        var gm = GameManager.Instance;
        string levelId = gm.PendingLevelId;
        int carIdx = gm.PendingCarIndex;

        if (!string.IsNullOrEmpty(levelId))
        {
            trackId = levelId;
        }

        UnloadLevelInstance();

        var levelPrefab = trackCatalog.GetLevelPrefab(levelId);
        if (levelPrefab == null)
        {
            LogClass.LogWarning(GameLogCategory.System, $"RcCarRaceGameMode: no level prefab for id '{levelId}'");
            return;
        }

        _levelInstance = Instantiate(levelPrefab, levelAnchor);
        var root = _levelInstance.GetComponent<RcRaceLevelRoot>();
        Transform spawn = root != null ? root.CarSpawn : null;
        if (spawn == null)
        {
            LogClass.LogError(GameLogCategory.System, "RcCarRaceGameMode: level prefab missing CarSpawn / RcRaceLevelRoot");
            Destroy(_levelInstance);
            _levelInstance = null;
            return;
        }

        var carDef = carRoster.GetCar(carIdx);
        if (carDef == null || carDef.carPrefab == null)
        {
            LogClass.LogError(GameLogCategory.System, $"RcCarRaceGameMode: invalid car at index {carIdx}");
            Destroy(_levelInstance);
            _levelInstance = null;
            return;
        }

        var carGo = Instantiate(carDef.carPrefab, spawn.position, spawn.rotation, _levelInstance.transform);
        var rb = carGo.GetComponent<Rigidbody2D>();
        var ctrl = carGo.GetComponent<RcCarController2D>();
        var inp = carGo.GetComponent<RcCarInputSystemPlayer>();
        if (rb == null || ctrl == null || inp == null)
        {
            LogClass.LogError(GameLogCategory.System, "RcCarRaceGameMode: car prefab missing Rigidbody2D / RcCarController2D / RcCarInputSystemPlayer");
            Destroy(carGo);
            Destroy(_levelInstance);
            _levelInstance = null;
            return;
        }

        raceSession.BindPlayerCar(rb, ctrl, inp, uiJoystick);

        if (root != null && root.FinishTrigger != null)
        {
            raceSession.SetFinishTrigger(root.FinishTrigger);
        }

        if (root != null)
        {
            foreach (var fl in root.GetFinishLinesInLevel())
            {
                if (fl != null)
                {
                    fl.BindSessionAndCar(raceSession, rb.transform);
                }
            }
        }
    }

    void UnloadLevelInstance()
    {
        if (_levelInstance != null)
        {
            Destroy(_levelInstance);
            _levelInstance = null;
        }
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
        {
            return;
        }

        _pausedFromRace = false;
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }
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
            {
                pauseMenuRoot.SetActive(false);
            }
            ResumeGame();
        }

        UnloadLevelInstance();

        if (!string.IsNullOrEmpty(exitSceneName))
        {
            SceneManager.LoadScene(exitSceneName);
        }
        else
        {
            Application.Quit();
        }
    }

    /// <summary>仅在比赛中生效；已暂停时重复调用无效。</summary>
    public void TryPauseRace()
    {
        if (_pausedFromRace)
        {
            return;
        }
        if (raceSession == null || !raceSession.IsRacing)
        {
            return;
        }

        _pausedFromRace = true;
        StopGame();
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);
        }
    }

    /// <summary>关闭暂停菜单并恢复 timeScale；非本模式触发的暂停不会执行任何操作。</summary>
    public void ResumeRace()
    {
        if (!_pausedFromRace)
        {
            return;
        }

        _pausedFromRace = false;
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }
        ResumeGame();
    }

    protected override void GameStart() { }

    protected override void GameEnd(GameEndReason reason) { }

    protected override bool CheckGameReady() => true;

    protected override void M_OnDestroy()
    {
        UnloadLevelInstance();
        if (resultPlayAgainButton != null)
        {
            resultPlayAgainButton.onClick.RemoveListener(PlayAgainFromResult);
        }
        if (_pausedFromRace)
        {
            ResumeRace();
        }
        base.M_OnDestroy();
    }
}
