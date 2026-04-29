using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单第二步（在 <see cref="RcLevelSelectPanel"/> 之后）：左右切换在 roster 内选车，
/// 单套 Slider 进度条 + 车名/预览图；切换时用插值刷新进度以突出数值差异。
/// 进入比赛场景由 <see cref="GameManager.EnterRaceFromCarSelect"/> 处理。
/// </summary>
public class RcCarSelectPanel : BasePanel
{
    [SerializeField] RcCarRoster roster;
    [SerializeField] Button prevCarButton;
    [SerializeField] Button nextCarButton;
    [SerializeField] Text carNameText;

    [SerializeField] Text difficultyText;
    [SerializeField] Image carPreviewImage;
    [SerializeField] StatRowUi statDisplay;

    [Header("等级")]
    [Tooltip("等级 → 字体颜色 / 图标的样式库；留空则等级文字保留原色，仅显示文本")]
    [SerializeField] RcCarStatGradeStyleLibrary gradeStyles;

    [Header("属性条动画")]
    [Tooltip("进度插值快慢（越大越快）")]
    [SerializeField] float barLerpSharpness = 14f;

    [SerializeField] Button confirmButton;
    [SerializeField] Button backButton;

    /// <summary>
    /// 单条属性的 UI 装配：进度条 + 等级文本（可空）。后续要给等级背景/边框等扩展，只在这里加字段。
    /// </summary>
    [System.Serializable]
    public class StatBarUi
    {
        public Slider bar;
        [Tooltip("等级文本（可空）；脚本会按 percent 算出等级，写入 text 与字体颜色")]
        public Text gradeText;
    }

    [System.Serializable]
    public class StatRowUi
    {
        public StatBarUi speed;
        public StatBarUi grip;
        public StatBarUi accel;
    }

    int _selectedIndex;

    int _targetSpeedPct;
    int _targetGripPct;
    int _targetAccelPct;

    float _shownSpeedPct;
    float _shownGripPct;
    float _shownAccelPct;

    bool _snapBarsOnNextApply = true;

    void Awake()
    {
        if (prevCarButton != null)
        {
            prevCarButton.onClick.AddListener(SelectPreviousCar);
        }

        if (nextCarButton != null)
        {
            nextCarButton.onClick.AddListener(SelectNextCar);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmStartRace);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackToLevelSelect);
        }
    }

    void Update()
    {
        if (roster == null || statDisplay == null || _snapBarsOnNextApply)
        {
            return;
        }

        float t = 1f - Mathf.Exp(-barLerpSharpness * Time.unscaledDeltaTime);
        _shownSpeedPct = Mathf.Lerp(_shownSpeedPct, _targetSpeedPct, t);
        _shownGripPct = Mathf.Lerp(_shownGripPct, _targetGripPct, t);
        _shownAccelPct = Mathf.Lerp(_shownAccelPct, _targetAccelPct, t);

        if (Mathf.Abs(_shownSpeedPct - _targetSpeedPct) < 0.05f
            && Mathf.Abs(_shownGripPct - _targetGripPct) < 0.05f
            && Mathf.Abs(_shownAccelPct - _targetAccelPct) < 0.05f)
        {
            _shownSpeedPct = _targetSpeedPct;
            _shownGripPct = _targetGripPct;
            _shownAccelPct = _targetAccelPct;
        }

        ApplyStatBars(_shownSpeedPct, _shownGripPct, _shownAccelPct);
    }

    void OnDestroy()
    {
        if (prevCarButton != null)
        {
            prevCarButton.onClick.RemoveListener(SelectPreviousCar);
        }

        if (nextCarButton != null)
        {
            nextCarButton.onClick.RemoveListener(SelectNextCar);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmStartRace);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackToLevelSelect);
        }
    }

    void OnBackToLevelSelect()
    {
        AudioManager.GetInstance().PlayUiClose();
        UIManager.GetInstance().PopPanel();
    }

    void SelectPreviousCar()
    {
        AudioManager.GetInstance().PlayUiClick();
        StepSelection(-1);
    }

    void SelectNextCar()
    {
        AudioManager.GetInstance().PlayUiClick();
        StepSelection(1);
    }

    void StepSelection(int delta)
    {
        if (roster == null || roster.Count <= 0)
        {
            return;
        }

        int n = roster.Count;
        _selectedIndex = ((_selectedIndex + delta) % n + n) % n;
        _snapBarsOnNextApply = false;
        PullTargetsFromSelection();
        ApplyGradeTexts();
        RefreshNameAndPreview();
    }

    public override void OnEnter()
    {
        if (roster != null && roster.Count > 0)
        {
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, roster.Count - 1);
        }
        else
        {
            _selectedIndex = 0;
        }

        _snapBarsOnNextApply = true;
        PullTargetsFromSelection();
        _shownSpeedPct = _targetSpeedPct;
        _shownGripPct = _targetGripPct;
        _shownAccelPct = _targetAccelPct;
        ApplyStatBars(_shownSpeedPct, _shownGripPct, _shownAccelPct);
        ApplyGradeTexts();
        RefreshNameAndPreview();
    }

    public override void OnPause() { }

    public override void OnResume() { }

    public override void OnExit() { }

    void PullTargetsFromSelection()
    {
        var def = roster != null ? roster.GetCar(_selectedIndex) : null;
        if (def == null)
        {
            _targetSpeedPct = _targetGripPct = _targetAccelPct = 0;
            return;
        }

        _targetSpeedPct = def.speedPercent;
        _targetGripPct = def.gripPercent;
        _targetAccelPct = def.accelPercent;
    }

    void RefreshNameAndPreview()
    {
        var def = roster != null ? roster.GetCar(_selectedIndex) : null;

        if (carNameText != null)
        {
            carNameText.text = def != null ? def.displayName : "";
        }

        if (carPreviewImage != null)
        {
            if (def != null && def.previewSprite != null)
            {
                carPreviewImage.sprite = def.previewSprite;
                carPreviewImage.enabled = true;
            }
            else
            {
                carPreviewImage.sprite = null;
                carPreviewImage.enabled = false;
            }
        }

        if (difficultyText != null && def != null)
        {
            difficultyText.text = DifficultyDisplay(def.difficulty);
            // 颜色统一走全局 UiColorPalette；token 没配时保留原色，避免视觉跳变
            difficultyText.color = UiColorService.GetInstance()
                .Get(DifficultyToken(def.difficulty), difficultyText.color);
        }
    }

    // 难度展示映射本面板自用，不抽全局扩展；以后若多面板复用再上扩展类。
    static string DifficultyDisplay(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return "简单";
            case Difficulty.Normal: return "普通";
            case Difficulty.Hard: return "困难";
            case Difficulty.Extreme: return "极难";
            default: return "";
        }
    }

    static UiColorToken DifficultyToken(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return UiColorToken.Green;
            case Difficulty.Normal: return UiColorToken.Blue;
            case Difficulty.Hard: return UiColorToken.Purple;
            case Difficulty.Extreme: return UiColorToken.Red;
            default: return UiColorToken.None;
        }
    }

    /// <summary>
    /// 按 0–100 设定 Slider 比例；若 Slider 的 min/max 不是 0–100，则用 normalizedValue 映射整条轨道。
    /// </summary>
    void ApplyStatBars(float speedPct, float gripPct, float accelPct)
    {
        if (statDisplay == null)
        {
            return;
        }

        SetSliderFromPercent(statDisplay.speed != null ? statDisplay.speed.bar : null, speedPct);
        SetSliderFromPercent(statDisplay.grip != null ? statDisplay.grip.bar : null, gripPct);
        SetSliderFromPercent(statDisplay.accel != null ? statDisplay.accel.bar : null, accelPct);
    }

    /// <summary>
    /// 等级文字按 target 值（离散）即时刷新，不跟随 Slider 插值——避免切车时 "C → C+ → B → B+" 飞快跳变。
    /// </summary>
    void ApplyGradeTexts()
    {
        if (statDisplay == null)
        {
            return;
        }

        ApplyGradeText(statDisplay.speed, _targetSpeedPct);
        ApplyGradeText(statDisplay.grip, _targetGripPct);
        ApplyGradeText(statDisplay.accel, _targetAccelPct);
    }

    void ApplyGradeText(StatBarUi bar, int percent)
    {
        if (bar == null || bar.gradeText == null)
        {
            return;
        }

        if (gradeStyles == null)
        {
            // 没配 library 就不显示等级，避免硬编码阈值/颜色潜在不一致
            bar.gradeText.text = "";
            return;
        }

        var grade = gradeStyles.FromPercent(percent);
        bar.gradeText.text = grade.ToDisplay();
        bar.gradeText.color = gradeStyles.GetTextColor(grade, bar.gradeText.color);
    }

    static void SetSliderFromPercent(Slider slider, float pct0To100)
    {
        if (slider == null)
        {
            return;
        }

        float n = Mathf.Clamp01(pct0To100 / 100f);
        float v = Mathf.Lerp(slider.minValue, slider.maxValue, n);
        slider.SetValueWithoutNotify(v);
    }

    /// <summary>确认选车：交给 <see cref="GameManager"/> 写入意图并加载比赛场景。</summary>
    void OnConfirmStartRace()
    {
        if (GameManager.Instance == null)
        {
            LogClass.LogWarning(GameLogCategory.UIManager, "RcCarSelectPanel: GameManager 缺失，无法进入比赛。");
            return;
        }

        AudioManager.GetInstance().PlayUiClick();
        GameManager.Instance.EnterRaceFromCarSelect(_selectedIndex);
    }
}
