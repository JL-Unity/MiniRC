using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void CompleteEvent();
public delegate void UpdateEvent(float inRate);

public class Timer
{
    public Timer(float time, CompleteEvent OnCompeleted, UpdateEvent update = null, int loopTimes = 1, bool isAutoDestory = true, bool isInfiniteLoop = false)
    {
        _duration = time;
        _OnCompleted = OnCompeleted;
        _UpdateEvent = update;
        _loopTimes = loopTimes;
        _isAutoDestory = isAutoDestory;
        _isInfiniteLoop = isInfiniteLoop;
        _startTime = Time.time;
    }
    
    int _loopTimes;   //循环次数
    float _duration; //计时长度

    UpdateEvent _UpdateEvent; //tick事件，传入是当前时间占总时间的多少
    CompleteEvent _OnCompleted; //完成事件，loopTime多少次就会触发多少次
    
    bool _isAutoDestory = true;     // 计时结束后是否自动销毁
    bool _isInfiniteLoop = true;
    
    public bool IsPaused;

    float _pausedTime; //进入暂停那一刻的 Time.time，Resume 时据此累加暂停时长
    float _totalPausedDuration; //累计暂停时长，从经过时间里扣除，使暂停不计入计时
    float _startTime; //本轮开始计时刻（Time.time）

    // 有限循环全部完成、且不自动销毁时置 true：停止再触发，保留对象等待外部 Reset()
    bool _isCompleted;

    public bool IsEnd { get; private set; }
    //已经经过的时间
    public float Elapsed;
    public float Progress => Mathf.Clamp01(Elapsed / _duration);
    
    public void Update()
    {
        if (IsEnd || IsPaused || _isCompleted)
        {
            return;
        }

        Elapsed = Time.time - _startTime - _totalPausedDuration;

        if (Elapsed < _duration)
        {
            _UpdateEvent?.Invoke(Progress);
            return;
        }

        // 到达一个周期。无限循环：推进起点后持续触发，不涉及次数与销毁
        if (_isInfiniteLoop)
        {
            _startTime += _duration;
            _OnCompleted?.Invoke();
            return;
        }

        // 有限循环：消耗一次次数并回调
        _loopTimes--;
        _OnCompleted?.Invoke();

        if (_loopTimes > 0)
        {
            // 还有剩余周期，推进起点继续计时
            _startTime += _duration;
            return;
        }

        // 全部周期完成：自动销毁交给 TimerManager 移除；否则停在完成态等待 Reset()
        if (_isAutoDestory)
        {
            IsEnd = true;
        }
        else
        {
            _isCompleted = true;
        }
    }
    
    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }
        _pausedTime = Time.time;
        IsPaused = true;
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }
        _totalPausedDuration += (Time.time - _pausedTime);
        IsPaused = false;
    }
    /// <summary>
    /// 重启本次计时，不重新计算LoopTime
    /// </summary>
    public void Reset()
    {
        _startTime = Time.time;
        _totalPausedDuration = 0;
        Elapsed = 0;
        IsPaused = false;
        _isCompleted = false;
    }
}
