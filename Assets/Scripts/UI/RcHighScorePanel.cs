using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 最高记录面板：写死三个关卡的时间 Text，按 Inspector 里填的 levelId
/// 读 PlayerPrefs 的 "MiniRC_RcRace_BestTotal_{levelId}"（总完赛最佳，秒）。
/// 未完成过的关显示 "--"。
/// </summary>
public class RcHighScorePanel : BasePanel, IStartMenuPanelAnimation
{
    const string PlayerPrefsBestKeyPrefix = "MiniRC_RcRace_BestTotal_";
    const string MissingRecordText = "--";

    [Header("关卡目录（赛道名称从这里按 levelId 取，与 RcLevelSelectPanel 同源）")]
    [SerializeField] RcTrackCatalog catalog;

    [Header("三个关卡的 id（与 RcTrackCatalog 里 levelId 对齐）")]
    [SerializeField] string level1Id;
    [SerializeField] string level2Id;
    [SerializeField] string level3Id;
    [SerializeField] string level4Id;

    [SerializeField] Text level1NameLabel;
    [SerializeField] Text level2NameLabel;
    [SerializeField] Text level3NameLabel;
    [SerializeField] Text level4NameLabel;

    [Header("对应的时间 Text")]
    [SerializeField] Text level1TimeLabel;
    [SerializeField] Text level2TimeLabel;
    [SerializeField] Text level3TimeLabel;

    [SerializeField] Text level4TimeLabel;

    [Header("返回按钮")]
    [SerializeField] Button backButton;

    void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
    }

    public override void OnEnter()
    {
        ApplyRow(level1Id, level1TimeLabel, level1NameLabel);
        ApplyRow(level2Id, level2TimeLabel, level2NameLabel);
        ApplyRow(level3Id, level3TimeLabel, level3NameLabel);
        ApplyRow(level4Id, level4TimeLabel, level4NameLabel);
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

    /// <summary>给一行（赛道名 + 最佳时间）赋值；catalog/levelId 任一缺失时显示占位文本。</summary>
    void ApplyRow(string levelId, Text timeLabel, Text nameLabel)
    {
        // 赛道名：catalog 按 levelId 查 entry → displayName（留空回退 levelId）
        if (nameLabel != null)
        {
            var entry = catalog != null ? catalog.GetEntry(levelId) : null;
            if (entry != null)
            {
                nameLabel.text = string.IsNullOrEmpty(entry.displayName) ? entry.levelId : entry.displayName;
            }
            else
            {
                // catalog 没填 / levelId 未登记：兜底显示 levelId 而不是空白，方便定位配置缺漏
                nameLabel.text = string.IsNullOrEmpty(levelId) ? MissingRecordText : levelId;
            }
        }

        // 最佳时间：照旧从 PlayerPrefs 读
        if (timeLabel == null) return;
        if (string.IsNullOrEmpty(levelId))
        {
            timeLabel.text = MissingRecordText;
            return;
        }

        float best = PlayerPrefs.GetFloat(PlayerPrefsBestKeyPrefix + levelId, float.MaxValue);
        timeLabel.text = best >= float.MaxValue - 1f
            ? MissingRecordText
            : RcCarRaceSession2D.FormatTime(best);
    }
}
