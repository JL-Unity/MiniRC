using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "AI/State Sequence")]
public class StateSequence : ScriptableObject
{
    public enum LoopType
    {
        None, Restart, PingPong
    }
    [System.Serializable]
    public class StateConfig
    {
        public State state;
        public StateOverride overrides = new StateOverride(); //控制state的关键参数，防止state创造过多asset
    }
    
    public LoopType loopType = LoopType.None;
    
    //状态列表
    public List<StateConfig> states = new List<StateConfig>();
    
}