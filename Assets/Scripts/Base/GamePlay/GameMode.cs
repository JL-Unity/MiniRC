using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 只在对应玩法的场景里面，提供玩法需要的全局接口，胜利失败规则等
/// </summary>
public abstract class GameMode : MonoBehaviour
{
    protected bool isGameReady = false;
    public string PlayerTag;
    public bool isGameStop { get; protected set; } = false;
    
    private void Awake()
    {
        OnAwake();
    }

    protected virtual void OnAwake()
    {
        EventCenter.GetInstance().Subscribe<PlayerDeadMessage>(PlayerDied);
    }

    void OnDestroy()
    {
        M_OnDestroy();
    }
    
    protected virtual void M_OnDestroy()
    {
        EventCenter.GetInstance().Unsubscribe<PlayerDeadMessage>(PlayerDied);
    }
    
    void Start()
    {
        GameManager.Instance.RegistGameMode(this);
        OnStart();
    }
    
    /// <summary>
    /// 已经在GameManager里面注册GameMode
    /// 且时机在Start之后，manager的awake都已经完成
    /// </summary>
    protected virtual void OnStart() { }

    /// <summary>
    /// 开始游戏之后要做什么，不要直接调用
    /// </summary>
    protected abstract void GameStart();
    protected abstract void GameEnd(GameEndReason  reason);
    
    /// <summary>
    /// 尝试开始游戏，不要直接调用OnGameStart
    /// </summary>
    public void TriggerGameStart()
    {
        if(CheckGameReady()) GameStart();
    }
    
    /// <summary>
    /// 尝试结束游戏
    /// </summary>
    /// <param name="reason"></param>
    public void TriggerGameEnd(GameEndReason reason)
    {
        GameEnd(reason);
    }
    
    /// <summary>
    /// 游戏开始前置条件是否已经准备完成
    /// </summary>
    /// <returns></returns>
    protected abstract bool CheckGameReady();

    protected virtual void PlayerDied(PlayerDeadMessage message) { }

    public virtual void RespawnPlayer(object playerIndex)
    {
        EventCenter.GetInstance().Publish(new PlayerRespawnMessage(playerIndex));   
    }
    
    public virtual void StopGame()
    {
        isGameStop = true;
        Time.timeScale = 0;
        EventCenter.GetInstance().Publish(default(GameStopMessage));
        //暂停表现
        LogClass.LogGame(GameLogCategory.System,"Game stopped");
    }
    
    public virtual void ResumeGame()
    {
        isGameStop = false;
        Time.timeScale = 1;
        EventCenter.GetInstance().Publish(default(GameResumeMessage));
        //继续表现
    }
}
