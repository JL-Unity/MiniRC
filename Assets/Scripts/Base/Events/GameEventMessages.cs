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
