using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 常驻 HUD：圈数行（最多 5 行）+ 合计行 + 暂停按钮。
/// 由 <see cref="RcCarRaceGameMode"/> 在 OnStart 中 <see cref="Bind"/> 注入 Session/Mode 引用，
/// 通过 EventCenter 接收一次性事件（圈数配置、开始、完成一圈、重置、结束），
/// 每帧从 Session 公开属性读取实时「当前圈 / 合计」渲染。
/// </summary>
[DisallowMultipleComponent]
public class RcCarRaceHud : MonoBehaviour
{
    [Header("圈数行（最多 5 行，按 LapCount 显示前 N 行）")]
    [Tooltip("依次对应第 1~5 圈的 HUD 行；未用到的会被隐藏")]
    [SerializeField] Component[] hudLapLines = new Component[5];

    [Header("合计")]
    [SerializeField] Component hudTotalLine;

    [Header("控制")]
    [SerializeField] Button pauseButton;

    RcCarRaceSession2D _session;
    RcCarRaceGameMode _mode;

    Action<RaceLapsConfiguredMessage> _onLapsConfigured;
    Action<RaceStartedMessage> _onRaceStarted;
    Action<RaceLapCompletedMessage> _onLapCompleted;
    Action<RaceResetMessage> _onRaceReset;
    Action<RaceFinishedMessage> _onRaceFinished;

    static Type _tmpUguiType;

    static Type TmpUguiType
    {
        get
        {
            if (_tmpUguiType == null)
            {
                _tmpUguiType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            }
            return _tmpUguiType;
        }
    }

    public void Bind(RcCarRaceSession2D session, RcCarRaceGameMode mode)
    {
        _session = session;
        _mode = mode;
        // 当前已处于的状态可能是首帧输入前（刚 BindPlayerCar）——无论如何刷一次 HUD 保底
        RefreshAllLapRows();
        RefreshTotalRow();
    }

    void OnEnable()
    {
        _onLapsConfigured = OnLapsConfigured;
        _onRaceStarted = OnRaceStarted;
        _onLapCompleted = OnLapCompleted;
        _onRaceReset = OnRaceReset;
        _onRaceFinished = OnRaceFinished;

        EventCenter.GetInstance().Subscribe(_onLapsConfigured);
        EventCenter.GetInstance().Subscribe(_onRaceStarted);
        EventCenter.GetInstance().Subscribe(_onLapCompleted);
        EventCenter.GetInstance().Subscribe(_onRaceReset);
        EventCenter.GetInstance().Subscribe(_onRaceFinished);

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
        }
    }

    void OnDisable()
    {
        EventCenter.GetInstance().Unsubscribe(_onLapsConfigured);
        EventCenter.GetInstance().Unsubscribe(_onRaceStarted);
        EventCenter.GetInstance().Unsubscribe(_onLapCompleted);
        EventCenter.GetInstance().Unsubscribe(_onRaceReset);
        EventCenter.GetInstance().Unsubscribe(_onRaceFinished);

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(OnPauseClicked);
        }
    }

    void Update()
    {
        if (_session == null || !_session.IsRacing)
        {
            return;
        }

        // Racing 时实时滚动"当前圈"行与合计行；已完成的圈由 RaceLapCompletedMessage 固化，不会被覆盖
        int current = _session.LapsCompleted;
        if (current < _session.LapsPerRound && current < (hudLapLines?.Length ?? 0))
        {
            var label = hudLapLines[current];
            if (label != null)
            {
                SetUIText(label, $"第{current + 1}圈： {RcCarRaceSession2D.FormatTime(_session.CurrentLapElapsed)}");
            }
        }

        RefreshTotalRow();
    }

    void OnPauseClicked()
    {
        AudioManager.GetInstance().PlayUiClick();
        _mode?.TryPauseRace();
    }

    void OnLapsConfigured(RaceLapsConfiguredMessage msg)
    {
        ApplyLapRowVisibility(msg.LapCount);
        RefreshAllLapRows();
        RefreshTotalRow();
    }

    void OnRaceStarted(RaceStartedMessage _)
    {
        RefreshAllLapRows();
        RefreshTotalRow();
    }

    void OnLapCompleted(RaceLapCompletedMessage msg)
    {
        if (hudLapLines == null || msg.LapIndex < 0 || msg.LapIndex >= hudLapLines.Length)
        {
            return;
        }
        var label = hudLapLines[msg.LapIndex];
        if (label != null)
        {
            SetUIText(label, $"第{msg.LapIndex + 1}圈： {RcCarRaceSession2D.FormatTime(msg.LapTime)}");
        }
        RefreshTotalRow();
    }

    void OnRaceReset(RaceResetMessage _)
    {
        RefreshAllLapRows();
        RefreshTotalRow();
    }

    void OnRaceFinished(RaceFinishedMessage msg)
    {
        // 结束态下合计固化为最终时间；lap 行保持各圈最终值（已由 OnLapCompleted 写入）
        if (hudTotalLine != null)
        {
            SetUIText(hudTotalLine, "合计： " + RcCarRaceSession2D.FormatTime(msg.TotalTime));
        }
    }

    void ApplyLapRowVisibility(int lapCount)
    {
        if (hudLapLines == null)
        {
            return;
        }
        for (int i = 0; i < hudLapLines.Length; i++)
        {
            if (hudLapLines[i] != null)
            {
                hudLapLines[i].gameObject.SetActive(i < lapCount);
            }
        }
    }

    void RefreshAllLapRows()
    {
        if (hudLapLines == null || _session == null)
        {
            return;
        }
        int n = Mathf.Min(hudLapLines.Length, _session.LapsPerRound);
        for (int i = 0; i < n; i++)
        {
            var label = hudLapLines[i];
            if (label != null)
            {
                SetUIText(label, BuildLapLineText(i));
            }
        }
    }

    string BuildLapLineText(int index)
    {
        var state = _session.State;
        if (state == RcCarRaceSession2D.SessionState.WaitingFirstInput)
        {
            return $"第{index + 1}圈： --";
        }

        int completed = _session.LapsCompleted;
        if (index < completed)
        {
            return $"第{index + 1}圈： {RcCarRaceSession2D.FormatTime(_session.GetLapTime(index))}";
        }

        if (state == RcCarRaceSession2D.SessionState.Racing
            && index == completed
            && completed < _session.LapsPerRound)
        {
            return $"第{index + 1}圈： {RcCarRaceSession2D.FormatTime(_session.CurrentLapElapsed)}";
        }

        return $"第{index + 1}圈： --";
    }

    void RefreshTotalRow()
    {
        if (hudTotalLine == null || _session == null)
        {
            return;
        }

        if (_session.State == RcCarRaceSession2D.SessionState.WaitingFirstInput)
        {
            SetUIText(hudTotalLine, "合计： --");
        }
        else
        {
            SetUIText(hudTotalLine, "合计： " + RcCarRaceSession2D.FormatTime(_session.TotalElapsed));
        }
    }

    /// <summary>写 uGUI <see cref="Text"/>（含子物体上的 Text）；若装了 TMP 则顺带支持。</summary>
    static void SetUIText(Component c, string value)
    {
        if (c == null)
        {
            return;
        }

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
        {
            return;
        }

        var tmp = go.GetComponent(tmpType) ?? go.GetComponentInChildren(tmpType, true);
        if (tmp != null)
        {
            tmpType.GetProperty("text")?.SetValue(tmp, value);
        }
        else if (tmpType.IsInstanceOfType(c))
        {
            tmpType.GetProperty("text")?.SetValue(c, value);
        }
    }
}
