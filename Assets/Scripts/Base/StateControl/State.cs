using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 通用状态机类
/// </summary>
public class State : ScriptableObject
{
    public string stateName = "SimpleState";
    public bool isEnter;
    public virtual void EnterState(StateController controller) { }

    public virtual void ExitState() { }

    //检测状态切换等
    public virtual void Update() { }

    //逻辑
    public virtual void FixedUpdate() { }
    
}
