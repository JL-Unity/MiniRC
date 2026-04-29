using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private bool isGameStop = false;

    public static GameManager Instance;

    private GameMode currentGameMode;

    [SerializeField] [Tooltip("不选择关卡的默认关卡id")]
    string defaultLevelId = "default";

    [Header("菜单 → 比赛")]
    [SerializeField]
    [Tooltip("Race 场景的 SceneStateAsset（sceneName 指向 GameScene）；由 GameManager 统一加载，勿在 UI 面板配置")]
    SceneStateAsset raceSceneAsset;

    [Header("比赛 → 菜单")]
    [SerializeField]
    [Tooltip("退回主菜单 / Start 场景用的 SceneStateAsset（与进场对称，走 SceneStateController 时也会触发上一状态的 ExitState）")]
    SceneStateAsset mainMenuSceneAsset;

    [Header("音频（跨场景持有；AudioSource 建议挂本物体子层以继承 DontDestroyOnLoad）")]
    [Tooltip("暴露了 MasterVolume / BGMVolume / SFXVolume 的 AudioMixer 资产")]
    [SerializeField] AudioMixer audioMixer;
    [Tooltip("BGM 用 AudioSource（Output 指向 Mixer 的 BGM group，loop=true）；由 SceneStateAsset.EnterState 驱动播放")]
    [SerializeField] AudioSource bgmSource;
    [Tooltip("SFX 用 AudioSource（Output 指向 Mixer 的 SFX group，PlayOneShot 方式使用）")]
    [SerializeField] AudioSource sfxSource;

    /// <summary>菜单 <see cref="SetPendingRaceIntent"/> 写入；Race 场景内消费。</summary>
    string _pendingLevelId;
    int _pendingCarIndex;

    /// <summary>Race 关卡 id（菜单意图；未设置时用 defaultLevelId）。</summary>
    public string PendingLevelId =>
        string.IsNullOrEmpty(_pendingLevelId) ? defaultLevelId : _pendingLevelId;

    /// <summary>在 <see cref="RcCarRoster"/> 中的下标。</summary>
    public int PendingCarIndex => _pendingCarIndex;

    /// <summary>选关面板调用：只写入关卡 id，再打开选车面板。</summary>
    public void SetPendingLevelId(string levelId)
    {
        if (!string.IsNullOrEmpty(levelId))
        {
            _pendingLevelId = levelId;
        }
    }

    /// <summary>选车面板确认时调用：只更新车辆下标（关卡应先由 <see cref="SetPendingLevelId"/> 写入）。</summary>
    public void SetPendingCarIndex(int carIndex)
    {
        _pendingCarIndex = Mathf.Max(0, carIndex);
    }

    /// <summary>一步写入关卡+车辆（脚本或调试可用）。</summary>
    public void SetPendingRaceIntent(string levelId, int carIndex)
    {
        SetPendingLevelId(levelId);
        SetPendingCarIndex(carIndex);
    }

    /// <summary>
    /// <see cref="SceneStateController"/> 异步切场景进行中时勿重复发起点「进入比赛 / 回菜单」等，以免栈与 AsyncOperation 乱序。
    /// </summary>
    public bool IsSceneTransitionInProgress =>
        SceneStateController.Instance != null && SceneStateController.Instance.IsAsyncLoadInProgress;

    /// <summary>
    /// 选车界面确认：写入车辆下标并加载比赛场景（异步进度条或直连由本处与 <see cref="SceneStateController"/> 决定）。
    /// </summary>
    public void EnterRaceFromCarSelect(int carIndex)
    {
        if (IsSceneTransitionInProgress)
        {
            return;
        }

        SetPendingCarIndex(carIndex);
        LoadConfiguredRaceScene();
    }

    /// <summary>
    /// 比赛内退出回菜单：<see cref="RcCarRaceGameMode"/> 等调用；与 <see cref="EnterRaceFromCarSelect"/> 对称，
    /// 优先 <see cref="SceneStateController.StartLoadingScene"/>，否则按 <see cref="SceneStateAsset.sceneName"/> 直连。
    /// </summary>
    public void ReturnToMainMenuFromRace()
    {
        if (IsSceneTransitionInProgress)
        {
            return;
        }

        LoadSceneFromStateAsset(mainMenuSceneAsset, "GameManager: 未配置 mainMenuSceneAsset，无法退回主菜单。");
    }

    /// <summary>比赛场景加载：优先走 <see cref="SceneStateController"/>，否则按资源上的 sceneName 直连。</summary>
    void LoadConfiguredRaceScene()
    {
        LoadSceneFromStateAsset(raceSceneAsset, "GameManager: 未配置 raceSceneAsset，无法进入比赛场景。");
    }

    /// <summary>统一：有 Controller 则走状态切换（会先 Exit 当前 SceneState），否则按 sceneName 直连。</summary>
    void LoadSceneFromStateAsset(SceneStateAsset asset, string missingAssetWarning)
    {
        if (asset == null)
        {
            LogClass.LogWarning(GameLogCategory.System, missingAssetWarning);
            return;
        }

        if (SceneStateController.Instance != null)
        {
            SceneStateController.Instance.StartLoadingScene(asset);
            return;
        }

        if (!string.IsNullOrEmpty(asset.sceneName))
        {
            SceneManager.LoadScene(asset.sceneName);
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 120;
        // 单例模式防止同时有两个Controller
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    void Start()
    {
        // Awake 里马上 SetFloat 到 Mixer 有时不生效（Unity 已知行为），这里在第一帧再 apply 一次
        AudioManager.GetInstance().LoadAndApplySavedVolumes();
    }

    void Init()
    {
        PoolManager.GetInstance().Init();
        UIManager.GetInstance().Init();
        TimerManager.GetInstance().Init();
        SkillManager.GetInstance().Init();
        AudioManager.GetInstance().Init();
        AudioManager.GetInstance().Configure(audioMixer, bgmSource, sfxSource);
        UiColorService.GetInstance().Init();
    }

    public GameMode GetGameMode()
    {
        return currentGameMode;
    }

    void Update()
    {
        if (isGameStop)
        {
            return;
        }
        TimerManager.GetInstance().Update();
    }

    public void RegistGameMode(GameMode mode)
    {
        if (mode)
        {
            currentGameMode = mode;
        }
        EventCenter.GetInstance().Publish(default(GameModeSetMessage));
        LogClass.LogGame(GameLogCategory.System, "GameModeSetEvent posted");
    }

    public void Clear()
    {
        LogClass.LogGame(GameLogCategory.System, "GameManager Clear");
        PoolManager.GetInstance().Clear();
        UIManager.GetInstance().Clear();
        EventCenter.GetInstance().Clear();
        SkillManager.GetInstance().Clear();
        TimerManager.GetInstance().Clear();
    }
}
