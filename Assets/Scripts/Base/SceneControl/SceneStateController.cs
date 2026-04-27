using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// 场景管理机
/// 和常规的StateController有区别，因为状态转换需要加载好场景之后才能进行
/// 这个项目用状态模式来处理场景切换的必要性其实不大，因为逻辑基本都在GameMode和GameManager分离好了
/// 主要是存档和读档之类的可以写在场景结束
/// </summary>
public class SceneStateController: StateController
{  
    public static SceneStateController Instance { get; private set; }
    [Tooltip("初始化的时候切换到指定下标的场景")]
    public bool needChangeState = false;
    public SceneStateAsset ChangeState;
    [Tooltip("当前场景是需要的场景")]
    public bool needInitState = false;
    public SceneStateAsset InitState;
    //是否需要加载UI
    public bool isNeedLoadingUI = false;
    //加载进度条
    public string loadingPanelName = PanelPath.StartUpPath + "LoadingPanel";

    private LoadingPanel _loadingPanel;
    
    //异步加载场景
    private AsyncOperation _asyncOperation;
    //场景加载进度
    private float SceneLoadingRate;
    //是否允许切换
    private bool isAllowSceneChange = true;
    //[SerializeField] private OperationScene operationScene;
    //[SerializeField] private StartUpScene startUpScene;
    
    
    // Start is called before the first frame update
    protected override void OnStart()
    {
        // 单例模式防止同时有两个Controller
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        //加载并切换场景
        if (needChangeState)
        {
            StartLoadingScene(ChangeState);
        }
        //场景是当前场景，直接初始化
        else if(needInitState)
        {
            EnterStateWithoutLoading(InitState);
        }
    }

    void EnterStateWithoutLoading(SceneStateAsset sceneState)
    {
        _currentState = sceneState;
        _currentState.EnterState(this);
    }
    
    /// <summary>是否存在未完成的 <see cref="SceneManager.LoadSceneAsync"/>（含未 allowSceneActivation 的阶段）。</summary>
    public bool IsAsyncLoadInProgress => _asyncOperation != null;

    public void StartLoadingScene(SceneStateAsset sceneState)
    {
        if (sceneState == null)
        {
            return;
        }

        if (_asyncOperation != null)
        {
            LogClass.LogWarning(
                GameLogCategory.SceneStateController,
                "已有场景异步加载进行中，忽略重复切换请求。");
            return;
        }

        _currentState?.ExitState();

        _currentState = sceneState;
        rate = 0;
        fakeRate = 0;
        _asyncOperation = SceneManager.LoadSceneAsync(sceneState.sceneName);
        if (_asyncOperation != null)
        {
            _asyncOperation.allowSceneActivation = false;
            StartCoroutine(processSceneLoading());
            if(isNeedLoadingUI)
            {
                _loadingPanel = UIManager.GetInstance().PushPanel(loadingPanelName).GetComponent<LoadingPanel>();
                if(_loadingPanel != null)
                {
                    _loadingPanel.SetLoadingProgress(0);
                }
            }
        }
        else
        {
            LogClass.LogWarning(GameLogCategory.SceneStateController, $"Create asyncOperation failed, scene name: {sceneState.sceneName}");
        }
    }
    
    protected override void OnUpdate()
    {
        base.OnUpdate();
        if (_asyncOperation != null)
        {
            if (fakeRate <= rate)
            {
                //假装加载
                fakeRate = Mathf.SmoothDamp(fakeRate, rate, ref dampVelocity, 0.9f);
            }

            ProcessLoadingUI(fakeRate);
            if (fakeRate >= 0.99f && rate >= 0.9999f && isAllowSceneChange)
            {
                // 第一步：开闸 + 切 BGM。Unity 此后还要 1+ 帧才完成场景切换，
                // LoadingPanel 必须继续遮着，否则玩家会看到上一场景的画面闪一下
                if (!_asyncOperation.allowSceneActivation)
                {
                    _asyncOperation.allowSceneActivation = true;
                    _currentState.EnterState(this);
                }

                // 第二步：等到新场景真正激活完（Awake/OnEnable/Start 都跑过 → isDone=true），
                // 再 Pop LoadingPanel，玩家"揭开"看到的就是新场景第一帧
                if (_asyncOperation.isDone)
                {
                    _asyncOperation = null;
                    UIManager.GetInstance().PopPanel();
                }
            }
        }
    }

    private float fakeRate = 0; //UI展示的值
    private float rate = 0; //加载真实值
    private float dampVelocity;
    IEnumerator processSceneLoading()
    {
        while (_asyncOperation.progress < 0.9f)
        {
            rate = _asyncOperation.progress;
            fakeRate = rate;
            yield return null;
        }
        rate = 1;
    }

    public void ProcessLoadingUI(float rate)
    {
        if(isNeedLoadingUI && _loadingPanel != null)
        {
            _loadingPanel.SetLoadingProgress(rate);
        }
    }

    // public void EnterGame()
    // {
    //     if (operationScene != null)
    //     {
    //         StartLoadingScene(operationScene);//重新开始 
    //     }
    // }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void SetIsAllowSceneChange(bool isAllowSceneChange)
    {
        this.isAllowSceneChange =  isAllowSceneChange;
    }
    
    // public void ToMenu()
    // {
    //     if (startUpScene != null)
    //     {
    //         StartLoadingScene(startUpScene);
    //     }
    // }
}
