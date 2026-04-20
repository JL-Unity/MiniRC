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
    
    // Start is called before the first frame update
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
        //SaveManager.GetInstance().Init();
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
    // Update is called once per frame
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
        if (mode) currentGameMode = mode;
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
