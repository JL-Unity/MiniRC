using System;
using UnityEngine;

/// <summary>
/// 关卡内「等待首次输入」时的方向提示。订阅 <see cref="RaceStartedMessage"/> 隐藏、
/// <see cref="RaceResetMessage"/> 重新显示；与 GameMode/Session 解耦，关卡需要就挂一个。
/// </summary>
/// <remarks>
/// 重要：本脚本不要挂在被显隐的提示节点本身——SetActive(false) 会让脚本失活、订阅丢失，
/// 之后 Reset 事件就再也收不到。请挂在常驻物体（关卡根等）上，把提示节点拖到 <see cref="hintTarget"/>。
/// </remarks>
[DisallowMultipleComponent]
public class RcRaceStartHint : MonoBehaviour
{
    [Tooltip("要被显隐的提示节点（含 FadeLoop 动画等）；留空则控制本节点（注意上面 remarks 提醒的限制）")]
    [SerializeField] GameObject hintTarget;

    Action<RaceStartedMessage> _onStarted;
    Action<RaceResetMessage> _onReset;

    void Awake()
    {
        if (hintTarget == null)
        {
            hintTarget = gameObject;
        }
        SetVisible(true);
    }

    void OnEnable()
    {
        _onStarted = _ => SetVisible(false);
        _onReset = _ => SetVisible(true);
        EventCenter.GetInstance().Subscribe(_onStarted);
        EventCenter.GetInstance().Subscribe(_onReset);
    }

    void OnDisable()
    {
        if (_onStarted != null)
        {
            EventCenter.GetInstance().Unsubscribe(_onStarted);
            _onStarted = null;
        }
        if (_onReset != null)
        {
            EventCenter.GetInstance().Unsubscribe(_onReset);
            _onReset = null;
        }
    }

    void SetVisible(bool visible)
    {
        if (hintTarget != null && hintTarget.activeSelf != visible)
        {
            hintTarget.SetActive(visible);
        }
    }
}
