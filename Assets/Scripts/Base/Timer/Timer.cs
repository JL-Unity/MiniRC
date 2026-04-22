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

    float _pausedTime; //中途暂停计时
    float _totalPausedDuration; //偏差时间，目的是暂停之后能重新计时
    float _startTime; //开始计时间
    float _timeNow;//当前时间

    public bool IsEnd { get; private set; }
    //已经经过的时间
    public float Elapsed;
    public float Progress => Mathf.Clamp01(Elapsed / _duration);
    
    float now;
    public void Update()
    {
        if (IsEnd || IsPaused)
        {
            return;
        }
        
        Elapsed = IsPaused ? _pausedTime - _startTime - _totalPausedDuration 
            : Time.time - _startTime - _totalPausedDuration;
        
        if (Elapsed >= _duration)
        {
            if (_loopTimes > 0 || _isInfiniteLoop)
            {
                _startTime += _duration;
                _loopTimes--;
            }
            IsEnd = !_isInfiniteLoop && (_loopTimes == 0 && _isAutoDestory);
            _OnCompleted?.Invoke();
            return;
        }
        _UpdateEvent?.Invoke(Progress);
    }
    
    public void Pause() => IsPaused = true;

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
        IsPaused = false;
    }
}
