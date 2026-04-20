using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerManager : BaseManager<TimerManager>
{
    private List<Timer> timers = new List<Timer>();
    public Timer AddTimer(float time, CompleteEvent OnCompeleted, UpdateEvent updateEvent = null, int loopTimes = 1, bool isAutoDestory = true, bool isInfiniteLoop = false)
    {
        Timer timer = new Timer(time, OnCompeleted, updateEvent, loopTimes, isAutoDestory, isInfiniteLoop);
        timers.Add(timer);
        return timer;
    }
    public override void Init()
    {
    }

    public override void Update()
    {
        for (int i = timers.Count - 1; i >= 0; i--)
        {
            if (timers[i].IsEnd)
            {
                timers.RemoveAt(i);
            }
            else
            {
                timers[i].Update();
            }
        }

    }
    
    public override void Clear()
    {
        timers.Clear();
    }

    public void RemoveTimer(Timer timer)
    {
        if (timer != null && timers.Contains(timer))
        {
            timer.IsPaused = true;
            timers.Remove(timer);
            timer = null;
        }
    }
}
