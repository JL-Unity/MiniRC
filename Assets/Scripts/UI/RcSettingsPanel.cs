using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音频设置面板：三个 Slider（Master/BGM/SFX）双向绑定 <see cref="AudioManager"/> 的线性音量（0~1），
/// 并把当前百分比显示到对应 Text（"100%"）。默认音量在 AudioManager 里为 100%。
/// OnEnter 从 AudioManager 回填当前值并绑监听；OnExit 解绑（避免跨场景回调到已销毁实例）。
/// 预制体放 Resources/UI/StartUp/RcSettingsPanel.prefab，用 UIManager.PushPanel("UI/StartUp/RcSettingsPanel") 打开。
/// </summary>
public class RcSettingsPanel : BasePanel
{
    [Header("Sliders（Range 0~1）")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Header("Percent Labels（显示 \"100%\" 等；可选）")]
    [SerializeField] Text masterValueLabel;
    [SerializeField] Text bgmValueLabel;
    [SerializeField] Text sfxValueLabel;

    [Header("Buttons")]
    [SerializeField] Button closeButton;

    public override void OnEnter()
    {
        RefillFromManager();

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    public override void OnExit()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    public override void OnPause() { }
    public override void OnResume()
    {
        // 可能从其他面板返回回来，保险起见再回填一次
        RefillFromManager();
    }

    void RefillFromManager()
    {
        AudioManager am = AudioManager.GetInstance();
        // SetValueWithoutNotify 避免回填时触发 onValueChanged 反过来写一遍 PlayerPrefs
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(am.MasterVolume);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(am.BgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(am.SfxVolume);

        UpdateLabel(masterValueLabel, am.MasterVolume);
        UpdateLabel(bgmValueLabel, am.BgmVolume);
        UpdateLabel(sfxValueLabel, am.SfxVolume);
    }

    void OnMasterChanged(float v)
    {
        AudioManager.GetInstance().SetMasterVolume(v);
        UpdateLabel(masterValueLabel, v);
    }

    void OnBgmChanged(float v)
    {
        AudioManager.GetInstance().SetBgmVolume(v);
        UpdateLabel(bgmValueLabel, v);
    }

    void OnSfxChanged(float v)
    {
        AudioManager.GetInstance().SetSfxVolume(v);
        UpdateLabel(sfxValueLabel, v);
    }

    static void UpdateLabel(Text label, float linear01)
    {
        if (label == null)
        {
            return;
        }
        label.text = Mathf.RoundToInt(Mathf.Clamp01(linear01) * 100f) + "%";
    }

    void OnCloseClicked()
    {
        AudioManager.GetInstance().PlayUiClose();
        UIManager.GetInstance().PopPanel();
    }
}
