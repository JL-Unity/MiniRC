/// <summary>
/// 强类型事件载荷：一种消息一个类型，订阅/发布用泛型，避免 string 与 object 强转。
/// 无参「信号」用空 readonly struct，发布时用 default 或 new()。
/// </summary>
public readonly struct GameModeSetMessage { }

public readonly struct GameStartMessage { }

public readonly struct GameStopMessage { }

public readonly struct GameResumeMessage { }

public readonly struct PlayerRespawnMessage
{
    public readonly object PlayerIndex;

    public PlayerRespawnMessage(object playerIndex)
    {
        PlayerIndex = playerIndex;
    }
}

/// <summary>原 PlayerDeadEvent：双参合并为一个载荷。</summary>
public readonly struct PlayerDeadMessage
{
    public readonly object PlayerIndex;
    public readonly object DeadType;

    public PlayerDeadMessage(object playerIndex, object deadType)
    {
        PlayerIndex = playerIndex;
        DeadType = deadType;
    }
}

/// <summary>Session 读取完关卡圈数后发出：HUD 用于决定显示多少行 Lap。</summary>
public readonly struct RaceLapsConfiguredMessage
{
    public readonly int LapCount;

    public RaceLapsConfiguredMessage(int lapCount)
    {
        LapCount = lapCount;
    }
}

/// <summary>首次输入后进入计时状态：HUD 清零 Lap 显示并开始每帧滚动合计。</summary>
public readonly struct RaceStartedMessage { }

/// <summary>过终点完成一圈：HUD 在对应行固化该圈耗时。</summary>
public readonly struct RaceLapCompletedMessage
{
    public readonly int LapIndex;
    public readonly float LapTime;

    public RaceLapCompletedMessage(int lapIndex, float lapTime)
    {
        LapIndex = lapIndex;
        LapTime = lapTime;
    }
}

/// <summary>重开 / 刚绑定玩家车：HUD 清空为 "--"，ResultPanel 关闭。</summary>
public readonly struct RaceResetMessage { }

/// <summary>最后一圈结束：载荷已含最终时间、最佳与是否破纪录，UI 直接填充。</summary>
public readonly struct RaceFinishedMessage
{
    public readonly float TotalTime;
    public readonly float BestShownTime;
    public readonly bool NewRecord;

    public RaceFinishedMessage(float totalTime, float bestShownTime, bool newRecord)
    {
        TotalTime = totalTime;
        BestShownTime = bestShownTime;
        NewRecord = newRecord;
    }
}
