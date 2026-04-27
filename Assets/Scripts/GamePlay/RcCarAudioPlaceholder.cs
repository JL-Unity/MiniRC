using UnityEngine;

// -----------------------------------------------------------------------------
// 占位音频：在 Inspector 里没指定 AudioClip 时由 RcCarEngineAudio / RcCarSkidAudio
// 在运行时合成；不写文件、不进 git。后期把正式 wav 拖到字段上即可覆盖。
// · 引擎：电机嗡鸣——sin 基频 + 谐波 + AM 调制（PWM 残留的脉动颤动感）+ 少量高频噪声
// · 漂移：单极 IIR 低通的白噪声，模拟轮胎尖叫的"嘶——"，本身就是噪声，loop 接缝几乎不可察
// 所有正弦/调制频率都取整 Hz，1 秒采样恰好整周期，loop 首尾相接零相位跳变
// -----------------------------------------------------------------------------
public static class RcCarAudioPlaceholder
{
    const int SampleRate = 44100;
    const int OneSecondSamples = SampleRate;

    /// <summary>引擎稳态 loop 占位。委托给 RcCarEngineSoundProfile 的字段默认值，集中维护参数。</summary>
    public static AudioClip CreateEngineLoop()
    {
        // 内存里临时实例化一份 SO 拿默认值；不进资源系统、用完即扔
        var profile = ScriptableObject.CreateInstance<RcCarEngineSoundProfile>();
        AudioClip clip = profile.Synthesize();
        Object.Destroy(profile);
        return clip;
    }

    /// <summary>漂移 loop 占位（1 秒、单声道）。低通白噪声近似轮胎尖叫。</summary>
    public static AudioClip CreateSkidLoop()
    {
        float[] data = new float[OneSecondSamples];
        float prev = 0f;
        const float alpha = 0.5f;
        for (int i = 0; i < OneSecondSamples; i++)
        {
            float n = Random.value * 2f - 1f;
            // 一阶 IIR：y[n] = y[n-1] + α(x[n] - y[n-1])，α 越小越闷
            prev += alpha * (n - prev);
            data[i] = prev * 0.7f;
        }
        AudioClip clip = AudioClip.Create("RcCarSkidPlaceholder", OneSecondSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
