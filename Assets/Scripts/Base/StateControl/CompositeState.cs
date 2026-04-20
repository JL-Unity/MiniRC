using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 组合装填，比如Idle+攻击
/// </summary>
[CreateAssetMenu(menuName = "AI/Common/CompositeState")]
public class CompositeState : State
{
    public List<State> parallelStates = new List<State>();
    public override void EnterState(StateController controller) 
    {
        foreach(var state in parallelStates) 
        {
            state.EnterState(controller);
        }
    }

    public override void ExitState() 
    {
        foreach(var state in parallelStates) 
        {
            state.ExitState();
        }
    }

    public override void Update() 
    {
        foreach(var state in parallelStates) 
        {
            state.Update();
        }
    }
    
    public override void FixedUpdate() 
    {
        foreach(var state in parallelStates) 
        {
            state.FixedUpdate();
        }
    }
}

