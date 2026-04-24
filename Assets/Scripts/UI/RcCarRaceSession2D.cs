using System.Text;
using UnityEngine;

/// <summary>
/// 计时赛状态机与圈数数据：首次输入后开表、过终点逐圈计时、满圈后结算。
/// 不持有任何 UI 引用——进度通过 EventCenter 广播，HUD 每帧读公开属性渲染。
/// </summary>
[DisallowMultipleComponent]
public class RcCarRaceSession2D : MonoBehaviour
{
    [Header("车（正常流程动态赋值，在原场景测试可以直接拖）")]
    [SerializeField] Rigidbody2D carRigidbody;
    [SerializeField] RcCarController2D carController;
    [SerializeField] RcCarInputSystemPlayer inputPlayer;
    [Tooltip("与 RcCarController2D 的 UI 摇杆一致；可空则只读键盘/手柄")]
    [SerializeField] Joystick uiJoystick;
    [SerializeField] float inputDeadZone = 0.08f;

    [Header("终点线")]
    [SerializeField] Collider2D finishTrigger;

    [Header("圈数（正常流程动态赋值，在原场景测试可以直接改）")]
    [SerializeField, Range(1, 5)] int lapsPerRound = 3;

    Vector3 _spawnPosition;
    Quaternion _spawnRotation;
    float[] _lapTimes;

    public enum SessionState
    {
        WaitingFirstInput,
        Racing,
        Finished
    }

    SessionState _state;
    float _lapStartTime;
    bool _finishArmed;
    int _lapsCompleted;

    bool _playerBound;

    public bool IsPlayerBound => _playerBound;

    /// <summary>是否处于计时赛进行中。</summary>
    public bool IsRacing => _state == SessionState.Racing;

    /// <summary>是否允许打开暂停菜单：等待首帧输入或比赛中都可，已结束不可。</summary>
    public bool CanPause => _state != SessionState.Finished;

    public SessionState State => _state;
    public int LapsPerRound => lapsPerRound;
    public int LapsCompleted => _lapsCompleted;

    /// <summary>当前未封存圈已进行的秒数；仅在 Racing 时返回实时值，其余为 0。</summary>
    public float CurrentLapElapsed
    {
        get
        {
            if (_state != SessionState.Racing)
            {
                return 0f;
            }
            if (_lapsCompleted >= lapsPerRound)
            {
                return 0f;
            }
            return Time.time - _lapStartTime;
        }
    }

    /// <summary>已过圈累加 + 当前圈实时耗时；结束后固定为最终总时间。</summary>
    public float TotalElapsed => ComputeTotalElapsedIncludingCurrentLap();

    public float GetLapTime(int lapIndex)
    {
        if (_lapTimes == null || lapIndex < 0 || lapIndex >= _lapTimes.Length)
        {
            return 0f;
        }
        return _lapTimes[lapIndex];
    }

    void Awake()
    {
        if (carRigidbody != null)
        {
            _spawnPosition = carRigidbody.transform.position;
            _spawnRotation = carRigidbody.transform.rotation;
            _playerBound = true;
        }

        ConfigureLaps(lapsPerRound);
    }

    /// <summary>按关卡圈数设置 <see cref="lapsPerRound"/> 与 <see cref="_lapTimes"/>，并广播 <see cref="RaceLapsConfiguredMessage"/>。</summary>
    public void ConfigureLaps(int lapCount)
    {
        lapCount = Mathf.Clamp(lapCount, 1, 5);
        lapsPerRound = lapCount;
        _lapTimes = new float[lapCount];

        EventCenter.GetInstance().Publish(new RaceLapsConfiguredMessage(lapCount));
    }

    void Update()
    {
        if (!_playerBound)
        {
            return;
        }
        if (_state == SessionState.Finished)
        {
            return;
        }

        if (_state == SessionState.WaitingFirstInput)
        {
            if (HasAnyInput())
            {
                BeginRaceFromFirstInput();
            }
        }
    }

    static bool IsGameplayPaused()
    {
        if (GameManager.Instance == null)
        {
            return false;
        }
        var mode = GameManager.Instance.GetGameMode();
        return mode != null && mode.isGameStop;
    }

    void BeginRaceFromFirstInput()
    {
        _state = SessionState.Racing;
        _lapsCompleted = 0;
        _lapStartTime = Time.time;
        for (int i = 0; i < _lapTimes.Length; i++)
        {
            _lapTimes[i] = 0f;
        }

        if (finishTrigger != null && carRigidbody != null
            && finishTrigger.OverlapPoint(carRigidbody.position))
        {
            _finishArmed = false;
        }
        else
        {
            _finishArmed = true;
        }

        EventCenter.GetInstance().Publish(new RaceStartedMessage());
    }

    bool HasAnyInput()
    {
        if (inputPlayer != null)
        {
            if (inputPlayer.ReadMove().sqrMagnitude > inputDeadZone * inputDeadZone)
            {
                return true;
            }
            if (inputPlayer.ReadSprint())
            {
                return true;
            }
            if (inputPlayer.ReadReverse())
            {
                return true;
            }
        }

        if (uiJoystick != null)
        {
            if (Mathf.Abs(uiJoystick.Horizontal) > inputDeadZone)
            {
                return true;
            }
            if (Mathf.Abs(uiJoystick.Vertical) > inputDeadZone)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>运行时绑定玩家车辆（Race 场景内由 GameMode Instantiate 后调用）。</summary>
    public void BindPlayerCar(Rigidbody2D rb, RcCarController2D ctrl, RcCarInputSystemPlayer input, Joystick joystickOptional)
    {
        carRigidbody = rb;
        carController = ctrl;
        inputPlayer = input;
        if (joystickOptional != null)
        {
            uiJoystick = joystickOptional;
        }

        _spawnPosition = rb.transform.position;
        _spawnRotation = rb.transform.rotation;
        _playerBound = true;

        _state = SessionState.WaitingFirstInput;
        _lapsCompleted = 0;
        _finishArmed = false;
        for (int i = 0; i < _lapTimes.Length; i++)
        {
            _lapTimes[i] = 0f;
        }

        if (carController != null)
        {
            carController.enabled = true;
        }

        EventCenter.GetInstance().Publish(new RaceResetMessage());
    }

    /// <summary>关卡预制体提供的终点触发器。</summary>
    public void SetFinishTrigger(Collider2D trigger)
    {
        finishTrigger = trigger;
    }

    /// <summary>由终点 <see cref="RcCarFinishLine2D"/> 在玩家车辆进入时调用。</summary>
    public void NotifyFinishEnterFromCar()
    {
        if (!_playerBound)
        {
            return;
        }
        if (_state != SessionState.Racing)
        {
            return;
        }
        if (IsGameplayPaused())
        {
            return;
        }
        if (!_finishArmed)
        {
            return;
        }

        float now = Time.time;
        float dt = now - _lapStartTime;
        dt = RoundToHundredths(dt);
        _lapStartTime = now;

        int lapIndex = _lapsCompleted;
        if (lapIndex < _lapTimes.Length)
        {
            _lapTimes[lapIndex] = dt;
        }
        _lapsCompleted++;
        _finishArmed = false;

        EventCenter.GetInstance().Publish(new RaceLapCompletedMessage(lapIndex, dt));

        if (_lapsCompleted >= lapsPerRound)
        {
            EndRace();
        }
    }

    /// <summary>由终点在玩家车辆离开触发器时调用。</summary>
    public void NotifyFinishExitFromCar()
    {
        if (!_playerBound)
        {
            return;
        }
        if (_state != SessionState.Racing)
        {
            return;
        }
        if (IsGameplayPaused())
        {
            return;
        }
        _finishArmed = true;
    }

    void EndRace()
    {
        _state = SessionState.Finished;

        if (carController != null)
        {
            carController.enabled = false;
        }
        if (carRigidbody != null)
        {
            carRigidbody.linearVelocity = Vector2.zero;
            carRigidbody.angularVelocity = 0f;
        }

        float total = RoundToHundredths(ComputeTotalElapsedIncludingCurrentLap());
        float bestShown = total;
        bool newRecord = false;
        if (GameManager.Instance != null
            && GameManager.Instance.GetGameMode() is RcCarRaceGameMode rcMode)
        {
            rcMode.ResolveBestTotal(total, out bestShown, out newRecord);
        }

        EventCenter.GetInstance().Publish(new RaceFinishedMessage(total, bestShown, newRecord));
    }

    /// <summary>由 <see cref="RcCarRaceGameMode"/> 在「再来一局」或暂停内重开时调用。</summary>
    public void ResetRaceToWaitingAtSpawn()
    {
        if (!_playerBound)
        {
            return;
        }

        ResetCarToSpawn();

        _state = SessionState.WaitingFirstInput;
        _lapsCompleted = 0;
        _finishArmed = false;
        for (int i = 0; i < _lapTimes.Length; i++)
        {
            _lapTimes[i] = 0f;
        }

        if (carController != null)
        {
            carController.enabled = true;
        }

        EventCenter.GetInstance().Publish(new RaceResetMessage());
    }

    void ResetCarToSpawn()
    {
        if (carRigidbody == null)
        {
            return;
        }

        carRigidbody.transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        carRigidbody.linearVelocity = Vector2.zero;
        carRigidbody.angularVelocity = 0f;
    }

    float ComputeTotalElapsedIncludingCurrentLap()
    {
        float sum = 0f;
        if (_lapsCompleted < lapsPerRound)
        {
            for (int i = 0; i < _lapsCompleted; i++)
            {
                sum += _lapTimes[i];
            }
            if (_state == SessionState.Racing)
            {
                sum += Time.time - _lapStartTime;
            }
        }
        else
        {
            for (int i = 0; i < lapsPerRound; i++)
            {
                sum += _lapTimes[i];
            }
        }

        return sum;
    }

    public static float RoundToHundredths(float seconds)
    {
        return Mathf.Round(seconds * 100f) / 100f;
    }

    /// <summary>显示到 0.01 秒（百分之一秒），与 <see cref="RoundToHundredths"/> 一致。</summary>
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f || float.IsInfinity(seconds) || float.IsNaN(seconds))
        {
            return "--";
        }

        seconds = RoundToHundredths(seconds);
        int totalCenti = Mathf.RoundToInt(seconds * 100f);
        int m = totalCenti / 6000;
        int s = totalCenti / 100 % 60;
        int cs = totalCenti % 100;
        var sb = new StringBuilder(16);
        sb.Append(m);
        sb.Append(':');
        if (s < 10)
        {
            sb.Append('0');
        }
        sb.Append(s);
        sb.Append('.');
        if (cs < 10)
        {
            sb.Append('0');
        }
        sb.Append(cs);
        return sb.ToString();
    }
}
