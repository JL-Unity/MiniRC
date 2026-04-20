using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateController : PoolObject
{
    protected State _currentState;

    void Awake()
    {
        OnAwake();
    }
    protected virtual void OnAwake() {}
    void Start() 
    {
        OnStart();
    }
    
    protected virtual void OnStart() { }
    /// <summary>
    /// 切换状态
    /// 注意：如果在状态的Update/FixUpdate里面调用需要提前return函数，防止当前状态的后续逻辑执行！！
    /// </summary>
    protected virtual void TransitionTo(State nextState)
    {
        if (_currentState)
        {
            _currentState.isEnter = false;
            _currentState?.ExitState();
        }
        
        _currentState = nextState;

        if (_currentState)
        {
            _currentState.EnterState(this);
            _currentState.isEnter = true;
        }
    }
    
    void Update()
    {
        OnUpdate();
    }
    
    protected virtual void OnUpdate()
    {
        if (_currentState != null && _currentState.isEnter)
        {
            _currentState.Update();
        }
    }

    void FixedUpdate()
    {
        OnFixedUpdate();
    }

    protected virtual void OnFixedUpdate()
    {
        if (_currentState != null && _currentState.isEnter)
        {
            _currentState.FixedUpdate();
        }
    }
}
