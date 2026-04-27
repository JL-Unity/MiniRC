using System;
using UnityEngine;

// -----------------------------------------------------------------------------
// 车辆引擎声：单 loop + 包络（音量 attack/release）+ pitch 跟车速。
// · 怠速也响：Start 即 Play，初始音量 = idleVolume；油门 + 车速 → 上爬到 drivingVolume
// · 没油门时车速对音量的贡献按 coastVolumeWeight 削弱（避免松油滑行还很响）
// · 音量与 pitch 都按车速线性插值（idle..driving / pitchIdle..pitchMax）
// · 不感知漂移；漂移声由独立的 RcCarSkidAudio 负责
// · engineStartClip 留接口：填上后会在按油门的边沿 PlayOneShot 一次（业界做法 A 起音段）
// · 暂停（GameStopMessage）时 Pause；恢复时 UnPause；期间 Update 跳过避免包络漂位
// · AudioSource 应在 Inspector 里把 outputAudioMixerGroup 指向 Mixer 的 SFX Group，
//   这样会自然受 AudioManager 已经管好的 SFX 总线音量控制，不需要扩 AudioManager
// -----------------------------------------------------------------------------

[DisallowMultipleComponent]
public class RcCarEngineAudio : MonoBehaviour
{
    [Header("References")]
    [Tooltip("油门状态来源；留空则只按车速联动音量/音调")]
    [SerializeField] RcCarInputSystemPlayer inputPlayer;
    [Tooltip("车速来源；留空尝试自取")]
    [SerializeField] Rigidbody2D rb;
    [Tooltip("引擎 AudioSource；要求挂在车上，loop=true，output=Mixer.SFX")]
    [SerializeField] AudioSource engineSource;

    [Header("Clips")]
    [Tooltip("稳态循环段；最高优先级——填了就直接用这段 wav/clip")]
    [SerializeField] AudioClip engineLoopClip;
    [Tooltip("可选：拖一份 RcCarEngineSoundProfile 资产，运行时按它的参数合成 loop。" +
             "优先级：engineLoopClip > engineSoundProfile > 默认 placeholder")]
    [SerializeField] RcCarEngineSoundProfile engineSoundProfile;
    [Tooltip("起音段（业界做法 A）。空则跳过，组件退化为做法 B")]
    [SerializeField] AudioClip engineStartClip;
    [Tooltip("起音段相对 loop 段的音量倍率")]
    [Range(0f, 1.5f)][SerializeField] float startClipVolumeScale = 1.0f;

    [Header("Volume · 远低于 BGM 的底噪")]
    [Tooltip("没踩油门 + 静止时的底噪音量")]
    [Range(0f, 1f)][SerializeField] float idleVolume = 0.06f;
    [Tooltip("油门踩满 + 高速时的目标音量（仍远低于 BGM）")]
    [Range(0f, 1f)][SerializeField] float drivingVolume = 0.16f;
    [Tooltip("速度达此值视为音量满档")]
    [SerializeField] float speedForFullVolume = 9f;
    [Tooltip("没踩油门时车速对音量的贡献占比；0 = 没油门只剩底噪，1 = 与踩油门相同")]
    [Range(0f, 1f)][SerializeField] float coastVolumeWeight = 0.5f;
    [Tooltip("音量上行（变响）的过渡时间（秒）")]
    [SerializeField] float volumeAttack = 0.2f;
    [Tooltip("音量下行（变弱）的过渡时间（秒）")]
    [SerializeField] float volumeRelease = 0.5f;

    [Header("Pitch · 跟车速小跨度变化")]
    [Tooltip("怠速 pitch")]
    [SerializeField] float pitchIdle = 0.85f;
    [Tooltip("满档 pitch（小跨度，避免常规驾驶就显得'夸张'）")]
    [SerializeField] float pitchMax = 1.1f;
    [Tooltip("速度达此值时 pitch 取 pitchMax")]
    [SerializeField] float speedForFullPitch = 9f;
    [Tooltip("pitch 收敛速率：1 - exp(-rate·dt)；越大跟车速越紧")]
    [SerializeField] float pitchLerpRate = 6f;

    Action<GameStopMessage> _onGameStop;
    Action<GameResumeMessage> _onGameResume;
    bool _pausedByGame;
    float _curVolume;
    float _curPitch;
    bool _wasThrottlePressed;

    void Reset()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (inputPlayer == null)
        {
            inputPlayer = GetComponentInParent<RcCarInputSystemPlayer>();
        }
        if (engineSource == null)
        {
            engineSource = GetComponent<AudioSource>();
        }
    }

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody2D>();
        }
        if (inputPlayer == null)
        {
            inputPlayer = GetComponentInParent<RcCarInputSystemPlayer>();
        }
    }

    void Start()
    {
        if (engineSource == null)
        {
            LogClass.LogWarning(GameLogCategory.Audio, $"{nameof(RcCarEngineAudio)}: engineSource 未指定。");
            return;
        }
        if (engineLoopClip == null)
        {
            // 优先级：profile（用户配置的） > placeholder（默认占位）
            engineLoopClip = engineSoundProfile != null
                ? engineSoundProfile.Synthesize()
                : RcCarAudioPlaceholder.CreateEngineLoop();
        }

        engineSource.clip = engineLoopClip;
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        _curVolume = idleVolume;
        _curPitch = pitchIdle;
        engineSource.volume = _curVolume;
        engineSource.pitch = _curPitch;
        engineSource.Play();

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
        if (engineSource == null || !engineSource.isPlaying)
        {
            return;
        }
        engineSource.Pause();
        _pausedByGame = true;
    }

    void OnGameResume(GameResumeMessage _)
    {
        if (engineSource == null || !_pausedByGame)
        {
            return;
        }
        engineSource.UnPause();
        _pausedByGame = false;
    }

    void Update()
    {
        if (engineSource == null || engineLoopClip == null)
        {
            return;
        }
        if (_pausedByGame)
        {
            // 暂停期间不动包络；恢复后从原状态继续，避免 UnPause 后从错位音量/音调起步
            return;
        }

        bool sprint = inputPlayer != null && inputPlayer.ReadSprint();
        bool reverse = inputPlayer != null && inputPlayer.ReadReverse();
        bool throttle = sprint || reverse;

        // 起音段（做法 A）：仅在 startClip 非空且油门"按下边沿"触发一次
        if (engineStartClip != null && throttle && !_wasThrottlePressed)
        {
            engineSource.PlayOneShot(engineStartClip, Mathf.Max(0f, startClipVolumeScale));
        }
        _wasThrottlePressed = throttle;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        float speedNorm = Mathf.Clamp01(speed / Mathf.Max(0.01f, speedForFullVolume));

        // 没油门时削弱速度对音量的贡献：松油滑行不应该和踩油门一样响
        float effectiveSpeedNorm = throttle ? speedNorm : speedNorm * coastVolumeWeight;
        float targetVol = Mathf.Lerp(idleVolume, drivingVolume, effectiveSpeedNorm);

        // 上行用 attack、下行用 release，速率按"满量程 / 时间"换算成"单位/秒"
        float dt = Time.deltaTime;
        float duration = targetVol >= _curVolume ? volumeAttack : volumeRelease;
        float ratePerSec = Mathf.Abs(drivingVolume - idleVolume) / Mathf.Max(0.01f, duration);
        _curVolume = Mathf.MoveTowards(_curVolume, targetVol, ratePerSec * dt);
        engineSource.volume = _curVolume;

        float pitchSpeedNorm = Mathf.Clamp01(speed / Mathf.Max(0.01f, speedForFullPitch));
        float targetPitch = Mathf.Lerp(pitchIdle, pitchMax, pitchSpeedNorm);
        // 指数收敛：与帧率无关，越大跟车速越紧
        float pitchT = Mathf.Clamp01(1f - Mathf.Exp(-pitchLerpRate * dt));
        _curPitch = Mathf.Lerp(_curPitch, targetPitch, pitchT);
        engineSource.pitch = _curPitch;
    }
}
