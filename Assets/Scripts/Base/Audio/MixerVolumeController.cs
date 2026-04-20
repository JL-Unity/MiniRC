using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 通过 AudioMixer 暴露参数控制总线音量；UI 滑条传 0~1 线性值即可。
/// 使用步骤：1) 创建 AudioMixer，对要控制的 Group 的 Volume 右键 Expose；2) 将暴露名填到下方字段；
/// 3) 场景内 AudioSource 的 Output 指向对应 Mixer Group。
/// </summary>
public class MixerVolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    [Header("须与 Mixer 中 Exposed Parameter 名称完全一致")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string bgmVolumeParam = "BGMVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    private const float MinDb = -80f;

    /// <summary>0 = 静音，1 = 0dB（近似）。</summary>
    public void SetGroupVolumeLinear(string exposedParameterName, float linear01)
    {
        if (audioMixer == null || string.IsNullOrEmpty(exposedParameterName))
        {
            return;
        }

        float db = Linear01ToDecibels(linear01);
        audioMixer.SetFloat(exposedParameterName, db);
    }

    public void SetMasterVolume(float linear01) => SetGroupVolumeLinear(masterVolumeParam, linear01);
    public void SetBgmVolume(float linear01) => SetGroupVolumeLinear(bgmVolumeParam, linear01);
    public void SetSfxVolume(float linear01) => SetGroupVolumeLinear(sfxVolumeParam, linear01);

    public static float Linear01ToDecibels(float linear01)
    {
        if (linear01 <= 0.0001f)
        {
            return MinDb;
        }

        return Mathf.Clamp(20f * Mathf.Log10(linear01), MinDb, 0f);
    }
}
