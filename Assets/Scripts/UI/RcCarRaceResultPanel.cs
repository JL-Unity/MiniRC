using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结果面板：由 <see cref="RcCarRaceGameMode"/> 在收到 <see cref="RaceFinishedMessage"/> 后 Push；
/// OnEnter 时从 <see cref="RcCarRaceGameMode.LastRaceResult"/> 读最新成绩填 UI。
/// </summary>
public class RcCarRaceResultPanel : BasePanel
{
    [Tooltip("本次成绩时间文本：只填 mm:ss.xx 数字；前缀'本次成绩'放 prefab 旁边的静态 Text")]
    [SerializeField] Text currentTimeLabel;
    [Tooltip("最好成绩时间文本：只填 mm:ss.xx 数字；前缀'最好成绩'放 prefab 旁边的静态 Text")]
    [SerializeField] Text bestTimeLabel;
    [Tooltip("破纪录时激活；非破纪录保持隐藏")]
    [SerializeField] GameObject newRecordIndicator;
    [SerializeField] Button playAgainButton;
    [Tooltip("返回开始菜单按钮：交给 GameMode.ExitRace() 处理场景切换")]
    [SerializeField] Button backToMenuButton;

    [Header("等级图标视图")]
    [Tooltip("本次成绩对应的等级图标视图")]
    [SerializeField] RcRaceGradeIconView currentGradeView;
    [Tooltip("最好成绩对应的等级图标视图")]
    [SerializeField] RcRaceGradeIconView bestGradeView;

    [Header("等级配色")]
    [Tooltip("等级 → 字体颜色 的映射 SO；缺失时时间文本保留 prefab 设计时颜色")]
    [SerializeField] RcRaceGradeStyleLibrary styleLibrary;

    RcCarRaceGameMode Mode => GameManager.Instance?.GetGameMode() as RcCarRaceGameMode;

    // 染色前先缓存 prefab 设计时颜色，作为库未拖 / 库未配 / grade 为 None 时的回退色；染过一次后就读不到原色
    Color _currentTimeDefaultColor = Color.white;
    Color _bestTimeDefaultColor = Color.white;
    bool _currentTimeColorCached;
    bool _bestTimeColorCached;

    public override void OnEnter()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        var mode = Mode;
        if (mode != null)
        {
            var r = mode.LastRaceResult;
            ApplyTimeLabel(currentTimeLabel, RcCarRaceSession2D.FormatTime(r.Total),
                r.CurrentGrade, ref _currentTimeDefaultColor, ref _currentTimeColorCached);
            ApplyTimeLabel(bestTimeLabel, RcCarRaceSession2D.FormatTime(r.BestShown),
                r.BestGrade, ref _bestTimeDefaultColor, ref _bestTimeColorCached);
            if (newRecordIndicator != null)
            {
                newRecordIndicator.SetActive(r.NewRecord);
            }
            if (currentGradeView != null) currentGradeView.SetGrade(r.CurrentGrade);
            if (bestGradeView != null) bestGradeView.SetGrade(r.BestGrade);
        }
    }

    void ApplyTimeLabel(Text label, string text, RcRaceGrade grade, ref Color defaultColor, ref bool cached)
    {
        if (label == null) return;

        label.text = text;

        if (!cached)
        {
            defaultColor = label.color;
            cached = true;
        }

        label.color = styleLibrary != null
            ? styleLibrary.GetTextColor(grade, defaultColor)
            : defaultColor;
    }

    public override void OnExit()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
        }
        if (newRecordIndicator != null)
        {
            newRecordIndicator.SetActive(false);
        }
        // 复位等级图标，避免面板复用时短暂闪现上一次等级
        if (currentGradeView != null) currentGradeView.SetGrade(RcRaceGrade.None);
        if (bestGradeView != null) bestGradeView.SetGrade(RcRaceGrade.None);
    }

    public override void OnPause() { }

    public override void OnResume() { }

    void OnPlayAgainClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        // 先关自己，再让 Mode 复位 Session（顺序无关紧要，但先 Pop 可以避免面板残留一帧）
        UIManager.GetInstance().PopPanel();
        Mode?.PlayAgainFromResult();
    }

    void OnBackToMenuClicked()
    {
        AudioManager.GetInstance().PlayUiClose();
        // 与「再来一局」一致：先关掉自己避免残留一帧，再让 Mode 走退出流程
        // ExitRace 内部仅在 _pausedFromRace 时才会再 Pop，结算面板不在暂停态，不会重复出栈
        UIManager.GetInstance().PopPanel();
        Mode?.ExitRace();
    }
}
