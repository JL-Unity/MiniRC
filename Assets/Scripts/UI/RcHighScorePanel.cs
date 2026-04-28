using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 记录面板：左右翻页浏览 <see cref="RcTrackCatalog"/> 中的赛道，展示赛道图/名 +
/// 玩家最佳时间与对应等级图标 + 该赛道各等级阈值。
/// 视觉骨架对齐 <see cref="RcLevelSelectPanel"/>，但纯查看，没有"确认进入"流程。
/// </summary>
public class RcHighScorePanel : BasePanel, IStartMenuPanelAnimation
{
    const string PlayerPrefsBestKeyPrefix = "MiniRC_RcRace_BestTotal_";
    const string MissingRecordText = "--";

    [Header("关卡目录")]
    [SerializeField] RcTrackCatalog catalog;

    [Header("切换与显示（与 RcLevelSelectPanel 同款骨架）")]
    [SerializeField] Button prevLevelButton;
    [SerializeField] Button nextLevelButton;
    [SerializeField] Text levelNameText;
    [SerializeField] Image levelPreviewImage;

    [Header("玩家成绩")]
    [Tooltip("显示当前关卡最佳总成绩；无成绩显示占位文本")]
    [SerializeField] Text bestTimeLabel;
    [Tooltip("当前关卡最佳成绩对应的等级图标；无成绩时由视图自身隐藏")]
    [SerializeField] RcRaceGradeIconView currentGradeView;
    [Tooltip("等级 → 字体颜色 的映射 SO；缺失时 bestTimeLabel 保持设计时颜色")]
    [SerializeField] RcRaceGradeStyleLibrary styleLibrary;

    [Header("等级阈值表（每行配对：等级 + 等级图标 + 时间要求文本）")]
    [SerializeField] GradeThresholdRow[] thresholdRows;

    [Header("返回按钮")]
    [SerializeField] Button backButton;

    /// <summary>阈值表的一行：声明该行代表哪个等级，并指定它要驱动的图标视图与时间文本。</summary>
    [Serializable]
    public class GradeThresholdRow
    {
        public RcRaceGrade grade;
        public RcRaceGradeIconView iconView;
        public Text thresholdText;
    }

    int _selectedIndex;
    // 染色前先缓存 prefab 设计时颜色，作为 None / 库未配 / 库未拖 时的回退色；染过一次后就读不到原色了
    Color _bestTimeLabelDefaultColor = Color.white;
    bool _bestTimeLabelColorCached;

    void Awake()
    {
        if (prevLevelButton != null) prevLevelButton.onClick.AddListener(SelectPreviousLevel);
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(SelectNextLevel);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

        if (bestTimeLabel != null)
        {
            _bestTimeLabelDefaultColor = bestTimeLabel.color;
            _bestTimeLabelColorCached = true;
        }
    }

    void OnDestroy()
    {
        if (prevLevelButton != null) prevLevelButton.onClick.RemoveListener(SelectPreviousLevel);
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(SelectNextLevel);
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
    }

    public override void OnEnter()
    {
        int count = catalog != null && catalog.tracks != null ? catalog.tracks.Length : 0;
        _selectedIndex = count > 0 ? Mathf.Clamp(_selectedIndex, 0, count - 1) : 0;

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
        AudioManager.GetInstance().PlayUiClose();
        PlayCloseAnimation();
    }

    void SelectPreviousLevel()
    {
        AudioManager.GetInstance().PlayUiClick();
        StepSelection(-1);
    }

    void SelectNextLevel()
    {
        AudioManager.GetInstance().PlayUiClick();
        StepSelection(1);
    }

    void StepSelection(int delta)
    {
        if (catalog == null || catalog.tracks == null || catalog.tracks.Length <= 0)
            return;

        int n = catalog.tracks.Length;
        // 模运算 +n 兜底，避免 -1 % n 在 C# 里得到负数
        _selectedIndex = ((_selectedIndex + delta) % n + n) % n;
        RefreshSelectionView();
    }

    RcTrackCatalog.TrackEntry GetCurrentEntry()
    {
        if (catalog == null || catalog.tracks == null || catalog.tracks.Length <= 0)
            return null;
        if (_selectedIndex < 0 || _selectedIndex >= catalog.tracks.Length)
            return null;
        return catalog.tracks[_selectedIndex];
    }

    void RefreshSelectionView()
    {
        var entry = GetCurrentEntry();

        ApplyLevelInfo(entry);
        ApplyBestRecord(entry);
        ApplyThresholdRows(entry);
    }

    void ApplyLevelInfo(RcTrackCatalog.TrackEntry entry)
    {
        if (levelNameText != null)
        {
            levelNameText.text = entry == null
                ? string.Empty
                : (string.IsNullOrEmpty(entry.displayName) ? entry.levelId : entry.displayName);
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
    }

    void ApplyBestRecord(RcTrackCatalog.TrackEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.levelId))
        {
            ApplyBestTimeLabel(MissingRecordText, RcRaceGrade.None);
            if (currentGradeView != null) currentGradeView.SetGrade(RcRaceGrade.None);
            return;
        }

        float best = PlayerPrefs.GetFloat(PlayerPrefsBestKeyPrefix + entry.levelId, float.MaxValue);
        bool hasRecord = best < float.MaxValue - 1f;

        RcRaceGrade grade = (hasRecord && catalog != null)
            ? catalog.EvaluateGrade(entry.levelId, best)
            : RcRaceGrade.None;

        ApplyBestTimeLabel(hasRecord ? RcCarRaceSession2D.FormatTime(best) : MissingRecordText, grade);

        if (currentGradeView != null) currentGradeView.SetGrade(grade);
    }

    void ApplyBestTimeLabel(string text, RcRaceGrade grade)
    {
        if (bestTimeLabel == null) return;

        bestTimeLabel.text = text;

        // 首次拿不到 prefab 颜色（Awake 时 bestTimeLabel 为 null 但运行期已被注入）的兜底缓存
        if (!_bestTimeLabelColorCached)
        {
            _bestTimeLabelDefaultColor = bestTimeLabel.color;
            _bestTimeLabelColorCached = true;
        }

        bestTimeLabel.color = styleLibrary != null
            ? styleLibrary.GetTextColor(grade, _bestTimeLabelDefaultColor)
            : _bestTimeLabelDefaultColor;
    }

    /// <summary>按当前赛道的阈值刷新阈值表：SS/S/A/B 显示对应上限秒数；
    /// C 是 EvaluateGrade 的兜底等级（无独立阈值），整行隐藏避免歧义。</summary>
    void ApplyThresholdRows(RcTrackCatalog.TrackEntry entry)
    {
        if (thresholdRows == null) return;

        for (int i = 0; i < thresholdRows.Length; i++)
        {
            var row = thresholdRows[i];
            if (row == null) continue;

            if (row.grade == RcRaceGrade.C)
            {
                // 图标走 IconView 的 hideWhenNone 通道隐藏；时间文本所在 GameObject 直接关掉
                if (row.iconView != null) row.iconView.SetGrade(RcRaceGrade.None);
                if (row.thresholdText != null && row.thresholdText.gameObject.activeSelf)
                {
                    row.thresholdText.gameObject.SetActive(false);
                }
                continue;
            }

            if (row.iconView != null) row.iconView.SetGrade(row.grade);

            if (row.thresholdText != null)
            {
                // 兜底：上一次若被 C 路径关过，下次配置改回非 C 时要重新激活
                if (!row.thresholdText.gameObject.activeSelf) row.thresholdText.gameObject.SetActive(true);
                row.thresholdText.text = entry == null
                    ? MissingRecordText
                    : RcCarRaceSession2D.FormatTime(GetGradeMaxSeconds(row.grade, entry));
            }
        }
    }

    static float GetGradeMaxSeconds(RcRaceGrade grade, RcTrackCatalog.TrackEntry entry)
    {
        switch (grade)
        {
            case RcRaceGrade.SS: return entry.gradeSsMaxSeconds;
            case RcRaceGrade.S:  return entry.gradeSMaxSeconds;
            case RcRaceGrade.A:  return entry.gradeAMaxSeconds;
            case RcRaceGrade.B:  return entry.gradeBMaxSeconds;
            default: return 0f;
        }
    }
}
