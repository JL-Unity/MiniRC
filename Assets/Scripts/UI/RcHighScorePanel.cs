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

    [Header("三个关卡的 id（与 RcTrackCatalog 里 levelId 对齐）")]
    [SerializeField] string level1Id;
    [SerializeField] string level2Id;
    [SerializeField] string level3Id;

    [Header("对应的时间 Text")]
    [SerializeField] Text level1TimeLabel;
    [SerializeField] Text level2TimeLabel;
    [SerializeField] Text level3TimeLabel;

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
        ApplyBestTime(level1Id, level1TimeLabel);
        ApplyBestTime(level2Id, level2TimeLabel);
        ApplyBestTime(level3Id, level3TimeLabel);
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

    static void ApplyBestTime(string levelId, Text label)
    {
        if (label == null) return;
        if (string.IsNullOrEmpty(levelId))
        {
            label.text = MissingRecordText;
            return;
        }

        float best = PlayerPrefs.GetFloat(PlayerPrefsBestKeyPrefix + levelId, float.MaxValue);
        label.text = best >= float.MaxValue - 1f
            ? MissingRecordText
            : RcCarRaceSession2D.FormatTime(best);
    }
}
