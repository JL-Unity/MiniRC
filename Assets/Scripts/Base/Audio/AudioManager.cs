using UnityEngine;
using UnityEngine.Audio;

// -----------------------------------------------------------------------------
// AudioManager（纯 C# 单例，与 UIManager / PoolManager 同层）
// 架构：
//   · 音量控制走 AudioMixer —— Master/BGM/SFX 三个 Group 分别暴露参数 MasterVolume/BGMVolume/SFXVolume
//   · 播放 AudioSource 挂在 GameManager 子物体上（GameManager 已 DontDestroyOnLoad），跨场景不丢
//   · 每个 clip 自己的基础音量通过 AudioSource.volume / PlayOneShot(..., scale) 单独传，与 Mixer 总线音量独立叠乘，不需要缓存
//   · 三档音量持久化到 PlayerPrefs，启动时回填 Mixer
// 使用：GameManager.Init 中调用 Configure 注入引用 → ApplySavedVolumes → PlayBGM(resPath)
// -----------------------------------------------------------------------------

public class AudioManager : BaseManager<AudioManager>
{
    // === 需与 Mixer 资产里 Expose 的参数名完全一致 ===
    public const string ExposedMasterParam = "MasterVolume";
    public const string ExposedBgmParam = "BGMVolume";
    public const string ExposedSfxParam = "SFXVolume";

    // === 常用 UI 音效路径（Resources 下；便捷方法 PlayUiClick/PlayUiClose 用）===
    public const string SfxUiClick = "Audio/SFX/Click";
    public const string SfxUiClose = "Audio/SFX/Close";

    // === PlayerPrefs ===
    const string PrefKeyMaster = "MiniRC_Audio_Master";
    const string PrefKeyBgm = "MiniRC_Audio_BGM";
    const string PrefKeySfx = "MiniRC_Audio_SFX";
    const float DefaultLinearVolume = 1f;

    // Mixer 参数的最小 dB（-80 约等于静音）
    const float MinDb = -80f;

    AudioMixer _mixer;
    AudioSource _bgmSource;
    AudioSource _sfxSource;

    float _masterLinear = DefaultLinearVolume;
    float _bgmLinear = DefaultLinearVolume;
    float _sfxLinear = DefaultLinearVolume;

    string _currentBgmPath;

    public float MasterVolume => _masterLinear;
    public float BgmVolume => _bgmLinear;
    public float SfxVolume => _sfxLinear;
    public string CurrentBgmPath => _currentBgmPath;
    public bool IsBgmPlaying => _bgmSource != null && _bgmSource.isPlaying;

    /// <summary>由 GameManager 注入 Mixer 与两个 AudioSource。必须在任何 Play/Set 前调用一次。</summary>
    public void Configure(AudioMixer mixer, AudioSource bgmSource, AudioSource sfxSource)
    {
        _mixer = mixer;
        _bgmSource = bgmSource;
        _sfxSource = sfxSource;
    }

    /// <summary>从 PlayerPrefs 读三档音量并写入 Mixer；启动时调一次。</summary>
    public void LoadAndApplySavedVolumes()
    {
        _masterLinear = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeyMaster, DefaultLinearVolume));
        _bgmLinear = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeyBgm, DefaultLinearVolume));
        _sfxLinear = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeySfx, DefaultLinearVolume));

        WriteMixer(ExposedMasterParam, _masterLinear);
        WriteMixer(ExposedBgmParam, _bgmLinear);
        WriteMixer(ExposedSfxParam, _sfxLinear);
    }

    public void SetMasterVolume(float linear01)
    {
        _masterLinear = Mathf.Clamp01(linear01);
        WriteMixer(ExposedMasterParam, _masterLinear);
        PlayerPrefs.SetFloat(PrefKeyMaster, _masterLinear);
    }

    public void SetBgmVolume(float linear01)
    {
        _bgmLinear = Mathf.Clamp01(linear01);
        WriteMixer(ExposedBgmParam, _bgmLinear);
        PlayerPrefs.SetFloat(PrefKeyBgm, _bgmLinear);
    }

    public void SetSfxVolume(float linear01)
    {
        _sfxLinear = Mathf.Clamp01(linear01);
        WriteMixer(ExposedSfxParam, _sfxLinear);
        PlayerPrefs.SetFloat(PrefKeySfx, _sfxLinear);
    }

    /// <summary>播放 BGM（循环）。resPath 为 Resources/ 下的相对路径，不带扩展名，例如 "Audio/BGM/Menu"。</summary>
    /// <param name="clipVolume">此 clip 自身的基础音量（0~1）；和 Mixer 的 BGM 档独立叠乘。</param>
    public void PlayBGM(string resPath, float clipVolume = 1f)
    {
        if (_bgmSource == null)
        {
            LogClass.LogWarning(GameLogCategory.Audio, "AudioManager.PlayBGM: bgmSource not configured");
            return;
        }
        if (string.IsNullOrEmpty(resPath))
        {
            return;
        }
        // 同一首在循环中不打断，免得滑条一动或重复切场景被 restart
        if (_currentBgmPath == resPath && _bgmSource.isPlaying)
        {
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(resPath);
        if (clip == null)
        {
            LogClass.LogWarning(GameLogCategory.Audio, $"AudioManager.PlayBGM: clip not found at '{resPath}'");
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.volume = Mathf.Clamp01(clipVolume);
        _bgmSource.Play();
        _currentBgmPath = resPath;
    }

    public void StopBGM()
    {
        if (_bgmSource != null && _bgmSource.isPlaying)
        {
            _bgmSource.Stop();
        }
        _currentBgmPath = null;
    }

    public void PauseBGM()
    {
        if (_bgmSource != null && _bgmSource.isPlaying)
        {
            _bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (_bgmSource != null && !_bgmSource.isPlaying && _bgmSource.clip != null)
        {
            _bgmSource.UnPause();
        }
    }

    /// <summary>播放一个音效（允许并发）。scale 为此次播放的基础音量（0~1）；和 Mixer SFX 档独立叠乘。</summary>
    public void PlaySFX(string resPath, float scale = 1f)
    {
        if (_sfxSource == null)
        {
            LogClass.LogWarning(GameLogCategory.Audio, "AudioManager.PlaySFX: sfxSource not configured");
            return;
        }
        if (string.IsNullOrEmpty(resPath))
        {
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(resPath);
        if (clip == null)
        {
            LogClass.LogWarning(GameLogCategory.Audio, $"AudioManager.PlaySFX: clip not found at '{resPath}'");
            return;
        }

        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(scale));
    }

    /// <summary>UI 按钮点击通用音效（非关闭类）。</summary>
    public void PlayUiClick() => PlaySFX(SfxUiClick);

    /// <summary>UI 关闭 / 返回 / 退出类按钮音效。</summary>
    public void PlayUiClose() => PlaySFX(SfxUiClose);

    void WriteMixer(string exposedParamName, float linear01)
    {
        if (_mixer == null || string.IsNullOrEmpty(exposedParamName))
        {
            return;
        }
        _mixer.SetFloat(exposedParamName, Linear01ToDecibels(linear01));
    }

    /// <summary>0 → -80dB（视作静音），1 → 0dB；低端走对数曲线，听感线性。</summary>
    public static float Linear01ToDecibels(float linear01)
    {
        if (linear01 <= 0.0001f)
        {
            return MinDb;
        }
        return Mathf.Clamp(20f * Mathf.Log10(Mathf.Clamp01(linear01)), MinDb, 0f);
    }

    public override void Clear()
    {
        // Mixer 参数与 PlayerPrefs 不清（跨场景保留）；AudioSource 随 GameManager 生命周期
    }
}
