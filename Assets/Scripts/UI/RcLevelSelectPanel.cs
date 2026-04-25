using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单第一步：左右切换在 <see cref="RcTrackCatalog"/> 内选关卡，样式对齐 <see cref="RcCarSelectPanel"/>。
/// 确认后写入 <see cref="GameManager.SetPendingLevelId"/>，再 <see cref="UIManager.PushPanel"/> 打开选车面板。
/// </summary>
public class RcLevelSelectPanel : BasePanel, IStartMenuPanelAnimation
{
    const string PlayerPrefsBestKeyPrefix = "MiniRC_RcRace_BestTotal_";
    const string MissingRecordText = "--";

    [Header("关卡目录")]
    [SerializeField] RcTrackCatalog catalog;

    [Tooltip("与 PoolManager/UIManager 一致的 prefab 资源名（通常在 Resources 下）")]
    [SerializeField] string carSelectPanelResourceName = "RcCarSelectPanel";

    [Header("切换与显示")]
    [SerializeField] Button prevLevelButton;
    [SerializeField] Button nextLevelButton;
    [SerializeField] Text levelNameText;
    [SerializeField] Image levelPreviewImage;
    [Tooltip("可选：显示当前关卡的最佳总成绩（PlayerPrefs 读取）")]
    [SerializeField] Text bestTimeLabel;

    [SerializeField] Button confirmButton;
    [SerializeField] Button backButton;

    int _selectedIndex;

    void Awake()
    {
        if (prevLevelButton != null)
        {
            prevLevelButton.onClick.AddListener(SelectPreviousLevel);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(SelectNextLevel);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmLevelChosen);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    void OnDestroy()
    {
        if (prevLevelButton != null)
        {
            prevLevelButton.onClick.RemoveListener(SelectPreviousLevel);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(SelectNextLevel);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmLevelChosen);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }

    public override void OnEnter()
    {
        int count = catalog != null && catalog.tracks != null ? catalog.tracks.Length : 0;
        if (count > 0)
        {
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, count - 1);
        }
        else
        {
            _selectedIndex = 0;
        }

        RefreshSelectionView();
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

    void SelectPreviousLevel()
    {
        StepSelection(-1);
    }

    void SelectNextLevel()
    {
        StepSelection(1);
    }

    void StepSelection(int delta)
    {
        if (catalog == null || catalog.tracks == null || catalog.tracks.Length <= 0)
        {
            return;
        }

        int n = catalog.tracks.Length;
        _selectedIndex = ((_selectedIndex + delta) % n + n) % n;
        RefreshSelectionView();
    }

    void RefreshSelectionView()
    {
        var entry = GetCurrentEntry();

        if (levelNameText != null)
        {
            if (entry == null)
            {
                levelNameText.text = "";
            }
            else
            {
                levelNameText.text = string.IsNullOrEmpty(entry.displayName) ? entry.levelId : entry.displayName;
            }
        }

        if (levelPreviewImage != null)
        {
            if (entry != null && entry.previewSprite != null)
            {
                levelPreviewImage.sprite = entry.previewSprite;
                levelPreviewImage.enabled = true;
            }
            else
            {
                levelPreviewImage.sprite = null;
                levelPreviewImage.enabled = false;
            }
        }

        ApplyBestTime(entry);
    }

    void ApplyBestTime(RcTrackCatalog.TrackEntry entry)
    {
        if (bestTimeLabel == null)
        {
            return;
        }

        if (entry == null || string.IsNullOrEmpty(entry.levelId))
        {
            bestTimeLabel.text = MissingRecordText;
            return;
        }

        float best = PlayerPrefs.GetFloat(PlayerPrefsBestKeyPrefix + entry.levelId, float.MaxValue);
        bestTimeLabel.text = best >= float.MaxValue - 1f
            ? MissingRecordText
            : RcCarRaceSession2D.FormatTime(best);
    }

    RcTrackCatalog.TrackEntry GetCurrentEntry()
    {
        if (catalog == null || catalog.tracks == null || catalog.tracks.Length <= 0)
        {
            return null;
        }

        if (_selectedIndex < 0 || _selectedIndex >= catalog.tracks.Length)
        {
            return null;
        }

        return catalog.tracks[_selectedIndex];
    }

    /// <summary>确认当前关卡：记下关卡 id，再压入选车面板。</summary>
    void OnConfirmLevelChosen()
    {
        var entry = GetCurrentEntry();
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
}
