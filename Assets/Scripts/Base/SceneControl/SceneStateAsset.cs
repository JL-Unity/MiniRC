using UnityEngine;

/// <summary>
/// 场景切换用的 ScriptableObject 状态（原命名 ISceneState 易与接口混淆，故改名）。
/// 在 Project 窗口 Create → MiniRC → Scene State Asset 创建资源。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/Scene State Asset", fileName = "SceneState")]
public class SceneStateAsset : State
{
    public string sceneName;

    [Header("BGM · 可选")]
    [Tooltip("Resources/ 下的相对路径（不带扩展名），例如 Audio/BGM/Menu；留空=不动当前 BGM")]
    public string bgmResPath;
    [Tooltip("勾上 = 进场时停掉 BGM（安静场景用）；优先级高于 bgmResPath")]
    public bool stopBgmOnEnter;

    public override void EnterState(StateController controller)
    {
        base.EnterState(controller);
        LogClass.LogGame(GameLogCategory.SceneStateController, "EnterState" + stateName);

        if (stopBgmOnEnter)
        {
            AudioManager.GetInstance().StopBGM();
        }
        else if (!string.IsNullOrEmpty(bgmResPath))
        {
            AudioManager.GetInstance().PlayBGM(bgmResPath);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        LogClass.LogGame(GameLogCategory.SceneStateController, "ExitState" + stateName);
        // 旧场景 GameObject 此刻还活着：清掉 UIManager 栈、PoolManager 池、场景级事件订阅，
        // 避免切场景后 PushPanel 触到上一场景被销毁的 BasePanel 引用。
        GameManager.Instance?.Clear();
    }
}
