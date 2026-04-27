using UnityEngine;

// -----------------------------------------------------------------------------
// 引擎声合成参数（电机嗡嗡声的 ScriptableObject 配置）
// · 暴露 RcCarAudioPlaceholder 里之前硬编码的所有参数，方便 Inspector 实时调
// · Synthesize() 在运行时合成一段 AudioClip（不写文件）；FillSamples() 复用给 wav 导出
// · 想要 loop 接缝零相位跳变：lengthSeconds × baseFreq / h2Freq / h3Freq / amFreq 的乘积都应是整数
//   推荐 lengthSeconds=1 + 所有频率取整 Hz
// -----------------------------------------------------------------------------

[CreateAssetMenu(menuName = "MiniRC/Engine Sound Profile", fileName = "RcCarEngineSoundProfile")]
public class RcCarEngineSoundProfile : ScriptableObject
{
    [Header("Output")]
    [Tooltip("生成 clip 长度（秒）；建议 1，配合所有整 Hz 的频率可让 loop 严丝合缝")]
    [Min(0.1f)] public float lengthSeconds = 1f;
    [Tooltip("采样率（Hz）；常用 44100/48000")]
    [Min(8000)] public int sampleRate = 44100;
    [Tooltip("生成 AudioClip 的名字 / 导出 wav 时的文件名前缀")]
    public string clipName = "RcCarEngineLoop";

    [Header("Tone · 主嗡鸣 + 谐波（取整 Hz）")]
    [Tooltip("基频（Hz）；pitch=1 时实际播放就是这个频率")]
    [Min(20f)] public float baseFreq = 220f;
    [Tooltip("基频的振幅权重")]
    [Range(0f, 1f)] public float fundamentalGain = 0.4f;
    [Tooltip("二次谐波频率（Hz）；一般取 2*baseFreq，给金属电流感")]
    [Min(0f)] public float h2Freq = 440f;
    [Range(0f, 1f)] public float h2Gain = 0.2f;
    [Tooltip("三次谐波频率（Hz）；一般取 3*baseFreq，给电流尖叫感")]
    [Min(0f)] public float h3Freq = 660f;
    [Range(0f, 1f)] public float h3Gain = 0.1f;

    [Header("AM Modulation · 脉动颤动感")]
    [Tooltip("AM 调制频率（Hz）；越高越'颤'（电机感），越低越'喘'（呼吸感）")]
    [Min(0f)] public float amFreq = 24f;
    [Tooltip("AM 调制深度；0=无 AM；0.18 = 振幅在 0.82..1.18 间脉动")]
    [Range(0f, 1f)] public float amDepth = 0.18f;

    [Header("Noise · 轴承摩擦感")]
    [Tooltip("噪声低通系数（用于近似高通白噪：原噪 - 低通噪）；越小越闷、越接近 1 越接近原始白噪")]
    [Range(0.01f, 0.99f)] public float noiseLpAlpha = 0.08f;
    [Tooltip("高频白噪混入权重；越大越'沙'，越小越纯净")]
    [Range(0f, 0.5f)] public float noiseGain = 0.02f;

    /// <summary>按当前参数合成一段 AudioClip（运行时用，不写文件）。</summary>
    public AudioClip Synthesize()
    {
        int rate = Mathf.Max(8000, sampleRate);
        int samples = Mathf.Max(8, Mathf.RoundToInt(rate * Mathf.Max(0.1f, lengthSeconds)));
        float[] data = new float[samples];
        FillSamples(data, rate);
        AudioClip clip = AudioClip.Create(
            string.IsNullOrEmpty(clipName) ? "RcCarEngineLoop" : clipName,
            samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>把当前参数渲染到给定 float[]（PCM 单声道，范围 [-1,1]）。供运行时合成与 wav 导出复用。</summary>
    public void FillSamples(float[] data, int rate)
    {
        if (data == null || data.Length == 0)
        {
            return;
        }
        float lpPrev = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / rate;
            float fundamental = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
            float h2 = Mathf.Sin(2f * Mathf.PI * h2Freq * t);
            float h3 = Mathf.Sin(2f * Mathf.PI * h3Freq * t);

            // 加权和：基频权重最大，谐波递减；总幅度留余量给 AM 上摆与噪声
            float core = fundamental * fundamentalGain + h2 * h2Gain + h3 * h3Gain;

            float am = 1f + amDepth * Mathf.Sin(2f * Mathf.PI * amFreq * t);

            // 高通白噪 = 原噪 - 单极 IIR 低通；近似 1 阶高通
            float n = Random.value * 2f - 1f;
            lpPrev += noiseLpAlpha * (n - lpPrev);
            float hpNoise = n - lpPrev;

            data[i] = core * am + hpNoise * noiseGain;
        }
    }
}
