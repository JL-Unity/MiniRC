using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单第一步：选关卡。确认某一关后写入 <see cref="GameManager.SetPendingLevelId"/>，
/// 再 <see cref="UIManager.PushPanel"/> 打开选车面板（Resources 路径名见 <see cref="carSelectPanelResourceName"/>）。
/// </summary>
public class RcLevelSelectPanel : BasePanel, IStartMenuPanelAnimation
{
    [Header("关卡目录")]
    [SerializeField] RcTrackCatalog catalog;

    [Tooltip("与 PoolManager/UIManager 一致的 prefab 资源名（通常在 Resources 下）")]
    [SerializeField] string carSelectPanelResourceName = "RcCarSelectPanel";

    [Header("与 catalog.tracks 顺序一一对应（几个关卡就拖几个按钮）")]
    [SerializeField] Button[] levelButtons;

    [Tooltip("可选：与 levelButtons 同序，用于显示关卡名")]
    [SerializeField] Text[] levelLabels;

    [SerializeField] Button backButton;

    public override void OnEnter()
    {
        BindButtonsIfNeeded();
        RefreshLabels();
        PlayOpenAnimation();
    }

    public void PlayOpenAnimation()
    {
        PanelAnimationUtil.TryPlayClip(GetComponent<Animation>(), PanelAnimationUtil.DefaultOpenClipName);
    }

    public void PlayCloseAnimation()
    {
        if (!PanelAnimationUtil.TryPlayClip(GetComponent<Animation>(), PanelAnimationUtil.DefaultCloseClipName))
        {
            OnCloseAnimationComplete();
        }
    }

    public void OnCloseAnimationComplete()
    {
        UIManager.GetInstance().PopPanel();
    }

    public override void OnPause() { }

    public override void OnResume() { }

    public override void OnExit() { }
    
    void OnBackClicked()
    {
        PlayCloseAnimation();
    }

    void BindButtonsIfNeeded()
    {
        if (catalog == null || catalog.tracks == null || levelButtons == null)
        {
            return;
        }
        if (backButton != null) 
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
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

        UIManager.GetInstance().PushPanel(PanelPath.StartUpPath + carSelectPanelResourceName);
    }

    void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
        }
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null)
            {
                continue;
            }
            levelButtons[i].onClick.RemoveAllListeners();
        }
    }
}
