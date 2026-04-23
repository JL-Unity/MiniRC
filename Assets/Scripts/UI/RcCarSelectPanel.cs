using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单第二步（在 <see cref="RcLevelSelectPanel"/> 之后）：左右切换在 roster 内选车，
/// 单套 Slider 进度条 + 车名/预览图；切换时用插值刷新进度以突出数值差异。
/// 进入比赛场景由 <see cref="GameManager.EnterRaceFromCarSelect"/> 处理。
/// </summary>
public class RcCarSelectPanel : BasePanel, IStartMenuPanelAnimation
{
    [SerializeField] RcCarRoster roster;
    [SerializeField] Button prevCarButton;
    [SerializeField] Button nextCarButton;
    [SerializeField] Text carNameText;
    [SerializeField] Image carPreviewImage;
    [SerializeField] StatRowUi statDisplay;

    [Header("属性条动画")]
    [Tooltip("进度插值快慢（越大越快）")]
    [SerializeField] float barLerpSharpness = 14f;

    [SerializeField] Button confirmButton;
    [SerializeField] Button backButton;

    [System.Serializable]
    public class StatRowUi
    {
        public Slider speedBar;
        public Slider gripBar;
        public Slider accelBar;
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
        PlayCloseAnimation();
    }

    void SelectPreviousCar()
    {
        StepSelection(-1);
    }

    void SelectNextCar()
    {
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
        RefreshNameAndPreview();
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

        SetSliderFromPercent(statDisplay.speedBar, speedPct);
        SetSliderFromPercent(statDisplay.gripBar, gripPct);
        SetSliderFromPercent(statDisplay.accelBar, accelPct);
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

        GameManager.Instance.EnterRaceFromCarSelect(_selectedIndex);
    }
}
