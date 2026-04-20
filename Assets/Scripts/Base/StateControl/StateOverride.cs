using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 状态参数覆盖,基类没有实际用途，子类需要填入参数用于转换
[System.Serializable]
public class StateOverride
{
    public bool overrideSpeed = false;
    public float speed;
    
    public bool overrideDuration = false;
    public float duration;
    
    public bool overrideDirection = false;
    public Vector2 direction;
}
