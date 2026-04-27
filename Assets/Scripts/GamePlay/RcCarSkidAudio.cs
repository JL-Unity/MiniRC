using System;
using UnityEngine;

// -----------------------------------------------------------------------------
// 车辆漂移声：单 loop + 包络。判定与 RcCarSkidEmitter 同构，但参数独立（声音可单独调）。
// · 漂中（横速 ≥ 阈值且总速 ≥ 最低速）→ 音量 attack 渐入；漂停 → release 渐出
// · 完全静音时 Stop()，下次起漂直接 Play()（漂移噪声 loop 接缝几乎不可察）
// · pitch 跟横速大小映射：越漂越尖
// · 暂停时 Pause；恢复时 UnPause（仅当我们之前主动暂停过）
// · 不耦合 RcCarSkidEmitter：避免单方依赖；如果以后要"声画严丝合缝"再切到共享判定
// -----------------------------------------------------------------------------

[DisallowMultipleComponent]
public class RcCarSkidAudio : MonoBehaviour
{
    [Header("References")]
    [Tooltip("车速/横速来源；留空尝试自取")]
    [SerializeField] Rigidbody2D rb;
    [Tooltip("漂移 AudioSource；要求挂在车上，loop=true，output=Mixer.SFX")]
    [SerializeField] AudioSource skidSource;

    [Header("Clip")]
    [Tooltip("漂移循环段；空则启动时合成占位音（RcCarAudioPlaceholder）")]
    [SerializeField] AudioClip skidLoopClip;

    [Header("Drift Detection")]
    [Tooltip("|v·right| ≥ 此值才算在漂；可独立于 SkidEmitter 调")]
    [SerializeField] float lateralSpeedThreshold = 2.5f;
    [Tooltip("总速 |v| 低于此不发声（防止极低速误判）")]
    [SerializeField] float minSpeedToEmit = 1.0f;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] float maxVolume = 0.55f;
    [Tooltip("起漂时音量上升过渡时间（秒）")]
    [SerializeField] float volumeAttack = 0.08f;
    [Tooltip("漂停时音量下降过渡时间（秒）")]
    [SerializeField] float volumeRelease = 0.3f;

    [Header("Pitch")]
    [SerializeField] float pitchAtThreshold = 0.95f;
    [SerializeField] float pitchAtMaxLateral = 1.15f;
    [Tooltip("横速达到此值时 pitch 取 pitchAtMaxLateral")]
    [SerializeField] float maxLateralForPitch = 6f;
    [SerializeField] float pitchLerpRate = 8f;

    Action<GameStopMessage> _onGameStop;
    Action<GameResumeMessage> _onGameResume;
    bool _pausedByGame;
    float _curVolume;
    float _curPitch;

    void Reset()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (skidSource == null)
        {
            skidSource = GetComponent<AudioSource>();
        }
    }

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody2D>();
        }
    }

    void Start()
    {
        if (skidSource == null)
        {
            LogClass.LogWarning(GameLogCategory.Audio, $"{nameof(RcCarSkidAudio)}: skidSource 未指定。");
            return;
        }
        if (skidLoopClip == null)
        {
            skidLoopClip = RcCarAudioPlaceholder.CreateSkidLoop();
        }

        skidSource.clip = skidLoopClip;
        skidSource.loop = true;
        skidSource.playOnAwake = false;
        skidSource.volume = 0f;
        skidSource.pitch = pitchAtThreshold;
        _curVolume = 0f;
        _curPitch = pitchAtThreshold;
        // 不立即 Play：等真的起漂再 Play，省 CPU；漂停后也 Stop

        _onGameStop = OnGameStop;
        _onGameResume = OnGameResume;
        EventCenter.GetInstance().Subscribe(_onGameStop);
        EventCenter.GetInstance().Subscribe(_onGameResume);
    }

    void OnDestroy()
    {
        if (_onGameStop != null)
        {
            EventCenter.GetInstance().Unsubscribe(_onGameStop);
        }
        if (_onGameResume != null)
        {
            EventCenter.GetInstance().Unsubscribe(_onGameResume);
        }
    }

    void OnGameStop(GameStopMessage _)
    {
        if (skidSource == null || !skidSource.isPlaying)
        {
            return;
        }
        skidSource.Pause();
        _pausedByGame = true;
    }

    void OnGameResume(GameResumeMessage _)
    {
        if (skidSource == null || !_pausedByGame)
        {
            return;
        }
        skidSource.UnPause();
        _pausedByGame = false;
    }

    void Update()
    {
        if (skidSource == null || skidLoopClip == null || rb == null)
        {
            return;
        }
        if (_pausedByGame)
        {
            return;
        }

        Vector2 v = rb.linearVelocity;
        // 横速取 |v·right|，与 RcCarController2D / RcCarSkidEmitter 同一坐标约定
        float lateral = Mathf.Abs(Vector2.Dot(v, transform.right));
        float speed = v.magnitude;
        bool drifting = lateral >= lateralSpeedThreshold && speed >= minSpeedToEmit;

        float targetVol = drifting ? maxVolume : 0f;
        float dt = Time.deltaTime;
        float duration = targetVol >= _curVolume ? volumeAttack : volumeRelease;
        float ratePerSec = maxVolume / Mathf.Max(0.01f, duration);
        _curVolume = Mathf.MoveTowards(_curVolume, targetVol, ratePerSec * dt);
        skidSource.volume = _curVolume;

        // pitch 由"超过阈值的部分横速"映射：横速 = 阈值时取 pitchAtThreshold
        float t = Mathf.Clamp01(
            (lateral - lateralSpeedThreshold) /
            Mathf.Max(0.01f, maxLateralForPitch - lateralSpeedThreshold));
        float targetPitch = Mathf.Lerp(pitchAtThreshold, pitchAtMaxLateral, t);
        float pitchT = Mathf.Clamp01(1f - Mathf.Exp(-pitchLerpRate * dt));
        _curPitch = Mathf.Lerp(_curPitch, targetPitch, pitchT);
        skidSource.pitch = _curPitch;

        // 完全静音 → 停 source 省 CPU；下次起漂再 Play
        if (_curVolume <= 0.0001f && skidSource.isPlaying)
        {
            skidSource.Stop();
        }
        else if (_curVolume > 0.001f && !skidSource.isPlaying)
        {
            skidSource.Play();
        }
    }
}
