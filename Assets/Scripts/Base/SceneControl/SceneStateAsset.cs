using UnityEngine;

/// <summary>
/// 场景切换用的 ScriptableObject 状态（原命名 ISceneState 易与接口混淆，故改名）。
/// 在 Project 窗口 Create → MiniRC → Scene State Asset 创建资源。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/Scene State Asset", fileName = "SceneState")]
public class SceneStateAsset : State
{
    public string sceneName;

    public override void EnterState(StateController controller)
    {
        base.EnterState(controller);
        LogClass.LogGame(GameLogCategory.SceneStateController, "EnterState" + stateName);
    }

    public override void ExitState()
    {
        base.ExitState();
        LogClass.LogGame(GameLogCategory.SceneStateController, "ExitState" + stateName);
    }
}
