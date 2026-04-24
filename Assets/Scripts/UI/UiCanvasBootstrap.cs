using UnityEngine;

/// <summary>
/// Transform 注入到 UIManager。
/// 每个需要 UI 的场景都要挂一份
/// </summary>
[DisallowMultipleComponent]
public class UiCanvasBootstrap : MonoBehaviour
{
    void Start()
    {
        if (GetComponent<Canvas>() == null)
        {
            LogClass.LogError(GameLogCategory.UIManager, "UiCanvasBootstrap: uiCanvas not exist");
            return;
        }
        UIManager.GetInstance().SetCanvasRoot(transform);
    }
}
