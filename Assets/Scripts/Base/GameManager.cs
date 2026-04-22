using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool isGameStop = false;

    public static GameManager Instance;

    private GameMode currentGameMode;

    [SerializeField] [Tooltip("可选：主 UI Canvas；不填则 UIManager 内 Find(\"Canvas\")")]
    private Transform uiCanvasRoot;

    [SerializeField] [Tooltip("不选择关卡的默认关卡id")]
    string defaultLevelId = "default";

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

    void Init()
    {
        PoolManager.GetInstance().Init();
        UIManager ui = UIManager.GetInstance();
        ui.SetCanvasRoot(uiCanvasRoot);
        ui.Init();
        TimerManager.GetInstance().Init();
        SkillManager.GetInstance().Init();
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
        Debug.Log("【GameManager】Clear");
        PoolManager.GetInstance().Clear();
        UIManager.GetInstance().Clear();
        EventCenter.GetInstance().Clear();
        SkillManager.GetInstance().Clear();
        TimerManager.GetInstance().Clear();
    }
}
