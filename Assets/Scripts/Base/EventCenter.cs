using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型事件总线：<c>Subscribe&lt;T&gt;</c> / <c>Publish&lt;T&gt;</c>，按消息类型 <see cref="Type"/> 分发。
/// <see cref="Clear"/> 仅移除「随场景清理」的订阅；常驻订阅用 <c>Subscribe(handler, clearOnSceneChange: false)</c>。
/// </summary>
public class EventCenter : BaseManager<EventCenter>
{
    private readonly Dictionary<Type, Delegate> _sceneScoped = new Dictionary<Type, Delegate>();
    private readonly Dictionary<Type, Delegate> _persistent = new Dictionary<Type, Delegate>();

    public override void Init()
    {
        base.Init();
    }

    /// <param name="clearOnSceneChange">true：场景切换时 <see cref="Clear"/> 会卸掉；false：与全局单例同寿命。</param>
    public void Subscribe<T>(Action<T> handler, bool clearOnSceneChange = true)
    {
        if (handler == null)
        {
            return;
        }

        var dict = clearOnSceneChange ? _sceneScoped : _persistent;
        Add(dict, typeof(T), handler);
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null)
        {
            return;
        }

        Remove(_sceneScoped, typeof(T), handler);
        Remove(_persistent, typeof(T), handler);
    }

    public void Publish<T>(T message)
    {
        var type = typeof(T);
        Invoke(_sceneScoped, type, message);
        Invoke(_persistent, type, message);
    }

    private static void Add(Dictionary<Type, Delegate> dict, Type type, Delegate handler)
    {
        if (dict.TryGetValue(type, out var existing))
        {
            dict[type] = Delegate.Combine(existing, handler);
        }
        else
        {
            dict[type] = handler;
        }
    }

    private static void Remove(Dictionary<Type, Delegate> dict, Type type, Delegate handler)
    {
        if (!dict.TryGetValue(type, out var existing))
        {
            return;
        }

        var next = Delegate.Remove(existing, handler);
        if (next == null || next.GetInvocationList().Length == 0)
        {
            dict.Remove(type);
        }
        else
        {
            dict[type] = next;
        }
    }

    private static void Invoke<T>(Dictionary<Type, Delegate> dict, Type type, T message)
    {
        if (!dict.TryGetValue(type, out var del))
        {
            return;
        }

        if (del is Action<T> action)
        {
            action.Invoke(message);
        }
    }

    /// <summary>场景切换时调用：只清空「随场景」的订阅。</summary>
    public override void Clear()
    {
        _sceneScoped.Clear();
        Debug.Log("【EventCenter】场景内事件订阅已清空");
    }
}
