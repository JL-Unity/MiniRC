using UnityEngine;

/// <summary>
/// 挂在 Race 场景：进入后将 UIManager 的 Canvas 根切到本场景赛道 UI。
/// </summary>
[DisallowMultipleComponent]
public class RcRaceUiBootstrap : MonoBehaviour
{
    [SerializeField] Canvas raceCanvas;

    void Start()
    {
        if (raceCanvas == null)
            return;
        UIManager.GetInstance().SetCanvasRoot(raceCanvas.transform);
    }
}
