using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 三圈计时：首次有输入时开始第 1 圈计时；每次过终点线记一圈（需离开终点后再进入下一圈）。
/// 总成绩为三圈时间之和；结束后禁用车辆，「再来一局」复位初始位与状态，下次仍由首次输入再开表。
/// </summary>
[DisallowMultipleComponent]
public class RcCarRaceSession2D : MonoBehaviour
{
    [Header("车")]
    [SerializeField] Rigidbody2D carRigidbody;
    [SerializeField] RcCarController2D carController;
    [SerializeField] RcCarInputSystemPlayer inputPlayer;
    [Tooltip("与 RcCarController2D 的 UI 摇杆一致；可空则只读键盘/手柄")]
    [SerializeField] Joystick uiJoystick;
    [SerializeField] float inputDeadZone = 0.08f;

    [Header("终点（用于判断首帧是否已在圈内，避免重复计圈）")]
    [SerializeField] Collider2D finishTrigger;

    [Header("圈数")]
    [SerializeField] int lapsPerRound = 3;

    [Header("HUD · 左上角（拖带 Text 的物体或 Text 组件；Text 在子物体上也可）")]
    [SerializeField] Component hudLap1Line;
    [SerializeField] Component hudLap2Line;
    [SerializeField] Component hudLap3Line;
    [SerializeField] Component hudTotalLine;

    [Header("结束面板")]
    [SerializeField] GameObject resultPanelRoot;
    [SerializeField] Component resultCurrentLine;
    [SerializeField] Component resultBestLine;

    Vector3 _spawnPosition;
    Quaternion _spawnRotation;
    float[] _lapTimes;

    enum SessionState
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

    /// <summary>是否处于计时赛进行中（可用于 GameMode 判断是否允许暂停）。</summary>
    public bool IsRacing => _state == SessionState.Racing;

    static Type _tmpUguiType;

    static Type TmpUguiType
    {
        get
        {
            if (_tmpUguiType == null)
                _tmpUguiType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            return _tmpUguiType;
        }
    }

    void Awake()
    {
        if (carRigidbody != null)
        {
            _spawnPosition = carRigidbody.transform.position;
            _spawnRotation = carRigidbody.transform.rotation;
            _playerBound = true;
        }

        _lapTimes = new float[Mathf.Max(1, lapsPerRound)];

        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(false);

        RefreshHud();
    }

    void Update()
    {
        if (!_playerBound)
            return;
        if (_state == SessionState.Finished)
            return;

        if (_state == SessionState.WaitingFirstInput)
        {
            if (HasAnyInput())
                BeginRaceFromFirstInput();
        }

        if (_state == SessionState.Racing && !IsGameplayPaused())
            UpdateHudLive();
    }

    static bool IsGameplayPaused()
    {
        if (GameManager.Instance == null)
            return false;
        var mode = GameManager.Instance.GetGameMode();
        return mode != null && mode.isGameStop;
    }

    void BeginRaceFromFirstInput()
    {
        _state = SessionState.Racing;
        _lapsCompleted = 0;
        _lapStartTime = Time.time;
        for (int i = 0; i < _lapTimes.Length; i++)
            _lapTimes[i] = 0f;

        if (finishTrigger != null && carRigidbody != null
            && finishTrigger.OverlapPoint(carRigidbody.position))
            _finishArmed = false;
        else
            _finishArmed = true;

        RefreshHud();
    }

    bool HasAnyInput()
    {
        if (inputPlayer != null)
        {
            if (inputPlayer.ReadMove().sqrMagnitude > inputDeadZone * inputDeadZone)
                return true;
            if (inputPlayer.ReadSprint())
                return true;
            if (inputPlayer.ReadReverse())
                return true;
        }

        if (uiJoystick != null)
        {
            if (Mathf.Abs(uiJoystick.Horizontal) > inputDeadZone)
                return true;
            if (Mathf.Abs(uiJoystick.Vertical) > inputDeadZone)
                return true;
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
            uiJoystick = joystickOptional;

        _spawnPosition = rb.transform.position;
        _spawnRotation = rb.transform.rotation;
        _playerBound = true;

        _state = SessionState.WaitingFirstInput;
        _lapsCompleted = 0;
        _finishArmed = false;
        for (int i = 0; i < _lapTimes.Length; i++)
            _lapTimes[i] = 0f;

        if (carController != null)
            carController.enabled = true;

        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(false);

        RefreshHud();
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
            return;
        if (_state != SessionState.Racing)
            return;
        if (IsGameplayPaused())
            return;
        if (!_finishArmed)
            return;

        float now = Time.time;
        float dt = now - _lapStartTime;
        dt = RoundToHundredths(dt);
        _lapStartTime = now;

        if (_lapsCompleted < _lapTimes.Length)
            _lapTimes[_lapsCompleted] = dt;
        _lapsCompleted++;
        _finishArmed = false;

        RefreshHud();

        if (_lapsCompleted >= lapsPerRound)
            EndRace();
    }

    /// <summary>由终点在玩家车辆离开触发器时调用。</summary>
    public void NotifyFinishExitFromCar()
    {
        if (!_playerBound)
            return;
        if (_state != SessionState.Racing)
            return;
        if (IsGameplayPaused())
            return;
        _finishArmed = true;
    }

    void EndRace()
    {
        _state = SessionState.Finished;

        if (carController != null)
            carController.enabled = false;
        if (carRigidbody != null)
        {
            carRigidbody.linearVelocity = Vector2.zero;
            carRigidbody.angularVelocity = 0f;
        }

        float total = RoundToHundredths(ComputeTotalElapsedIncludingCurrentLap());

        float bestShown = total;
        if (GameManager.Instance != null
            && GameManager.Instance.GetGameMode() is RcCarRaceGameMode rcMode)
            rcMode.ResolveBestTotal(total, out bestShown, out _);

        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(true);
        SetUIText(resultCurrentLine, "本次成绩： " + FormatTime(total));
        SetUIText(resultBestLine, "最好成绩： " + FormatTime(bestShown));

        RefreshHud();
    }

    /// <summary>由 <see cref="RcCarRaceGameMode"/> 在「再来一局」或暂停内重开时调用。</summary>
    public void ResetRaceToWaitingAtSpawn()
    {
        if (!_playerBound)
            return;
        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(false);

        ResetCarToSpawn();

        _state = SessionState.WaitingFirstInput;
        _lapsCompleted = 0;
        _finishArmed = false;
        for (int i = 0; i < _lapTimes.Length; i++)
            _lapTimes[i] = 0f;

        if (carController != null)
            carController.enabled = true;

        RefreshHud();
    }

    void ResetCarToSpawn()
    {
        if (carRigidbody == null)
            return;

        carRigidbody.transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        carRigidbody.linearVelocity = Vector2.zero;
        carRigidbody.angularVelocity = 0f;
    }

    void UpdateHudLive()
    {
        if (_state != SessionState.Racing)
            return;

        ApplyLapHudTexts();

        if (hudTotalLine == null)
            return;

        float sum = ComputeTotalElapsedIncludingCurrentLap();
        SetUIText(hudTotalLine, "合计： " + FormatTime(sum));
    }

    float ComputeTotalElapsedIncludingCurrentLap()
    {
        float sum = 0f;
        if (_lapsCompleted < lapsPerRound)
        {
            for (int i = 0; i < _lapsCompleted; i++)
                sum += _lapTimes[i];
            sum += Time.time - _lapStartTime;
        }
        else
        {
            for (int i = 0; i < lapsPerRound; i++)
                sum += _lapTimes[i];
        }

        return sum;
    }

    void RefreshHud()
    {
        ApplyLapHudTexts();

        if (hudTotalLine == null)
            return;

        if (_state == SessionState.WaitingFirstInput)
            SetUIText(hudTotalLine, "合计： --");
        else
            SetUIText(hudTotalLine, "合计： " + FormatTime(ComputeTotalElapsedIncludingCurrentLap()));
    }

    void ApplyLapHudTexts()
    {
        SetLapHudOne(hudLap1Line, 0);
        SetLapHudOne(hudLap2Line, 1);
        SetLapHudOne(hudLap3Line, 2);
    }

    void SetLapHudOne(Component label, int index)
    {
        if (label == null)
            return;
        if (index >= lapsPerRound)
        {
            SetUIText(label, "");
            return;
        }

        SetUIText(label, BuildLapLineText(index));
    }

    string BuildLapLineText(int index)
    {
        if (_state == SessionState.WaitingFirstInput)
            return $"第{index + 1}圈： --";

        if (index < _lapsCompleted)
            return $"第{index + 1}圈： {FormatTime(_lapTimes[index])}";

        if (_state == SessionState.Racing && index == _lapsCompleted && _lapsCompleted < lapsPerRound)
        {
            float elapsed = Time.time - _lapStartTime;
            return $"第{index + 1}圈： {FormatTime(elapsed)}";
        }

        return $"第{index + 1}圈： --";
    }

    public static float RoundToHundredths(float seconds)
    {
        return Mathf.Round(seconds * 100f) / 100f;
    }

    /// <summary>写 uGUI <see cref="Text"/>（含子物体上的 Text）；若装了 TMP 则顺带支持。</summary>
    static void SetUIText(Component c, string value)
    {
        if (c == null)
            return;

        if (c is Text leg)
        {
            leg.text = value;
            return;
        }

        var go = c.gameObject;
        var ugui = go.GetComponent<Text>() ?? go.GetComponentInChildren<Text>(true);
        if (ugui != null)
        {
            ugui.text = value;
            return;
        }

        var tmpType = TmpUguiType;
        if (tmpType == null)
            return;

        var tmp = go.GetComponent(tmpType) ?? go.GetComponentInChildren(tmpType, true);
        if (tmp != null)
            tmpType.GetProperty("text")?.SetValue(tmp, value);
        else if (tmpType.IsInstanceOfType(c))
            tmpType.GetProperty("text")?.SetValue(c, value);
    }

    /// <summary>显示到 0.01 秒（百分之一秒），与 <see cref="RoundToHundredths"/> 一致。</summary>
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f || float.IsInfinity(seconds) || float.IsNaN(seconds))
            return "--";

        seconds = RoundToHundredths(seconds);
        int totalCenti = Mathf.RoundToInt(seconds * 100f);
        int m = totalCenti / 6000;
        int s = totalCenti / 100 % 60;
        int cs = totalCenti % 100;
        var sb = new StringBuilder(16);
        sb.Append(m);
        sb.Append(':');
        if (s < 10)
            sb.Append('0');
        sb.Append(s);
        sb.Append('.');
        if (cs < 10)
            sb.Append('0');
        sb.Append(cs);
        return sb.ToString();
    }
}
