using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单第一步：选关卡。确认某一关后写入 <see cref="GameManager.SetPendingLevelId"/>，
/// 再 <see cref="UIManager.PushPanel"/> 打开选车面板（Resources 路径名见 <see cref="carSelectPanelResourceName"/>）。
/// </summary>
public class RcLevelSelectPanel : BasePanel
{
    [SerializeField] RcTrackCatalog catalog;

    [Tooltip("与 PoolManager/UIManager 一致的 prefab 资源名（通常在 Resources 下）")]
    [SerializeField] string carSelectPanelResourceName = "RcCarSelectPanel";

    [Header("与 catalog.tracks 顺序一一对应（几个关卡就拖几个按钮）")]
    [SerializeField] Button[] levelButtons;

    [Tooltip("可选：与 levelButtons 同序，用于显示关卡名")]
    [SerializeField] Text[] levelLabels;

    public override void OnEnter()
    {
        BindLevelButtonsIfNeeded();
        RefreshLabels();
    }

    public override void OnPause() { }

    public override void OnResume() { }

    public override void OnExit() { }

    void BindLevelButtonsIfNeeded()
    {
        if (catalog == null || catalog.tracks == null || levelButtons == null)
        {
            return;
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null)
            {
                continue;
            }
            levelButtons[i].onClick.RemoveAllListeners();
            if (i >= catalog.tracks.Length)
            {
                continue;
            }

            int idx = i;
            levelButtons[i].onClick.AddListener(() => OnLevelChosen(idx));
        }
    }

    void RefreshLabels()
    {
        if (catalog?.tracks == null || levelLabels == null)
        {
            return;
        }

        for (int i = 0; i < levelLabels.Length && i < catalog.tracks.Length; i++)
        {
            if (levelLabels[i] == null)
            {
                continue;
            }
            var e = catalog.tracks[i];
            if (e == null)
            {
                continue;
            }
            levelLabels[i].text = string.IsNullOrEmpty(e.displayName) ? e.levelId : e.displayName;
        }
    }

    /// <summary>玩家点某一关：记下关卡 id，再压入选车面板。</summary>
    void OnLevelChosen(int trackIndex)
    {
        if (catalog?.tracks == null || trackIndex < 0 || trackIndex >= catalog.tracks.Length)
        {
            return;
        }

        var entry = catalog.tracks[trackIndex];
        if (entry == null || string.IsNullOrEmpty(entry.levelId))
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPendingLevelId(entry.levelId);
        }

        UIManager.GetInstance().PushPanel(carSelectPanelResourceName);
    }
}
