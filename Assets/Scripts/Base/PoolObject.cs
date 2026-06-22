using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolObject : MonoBehaviour
{
    [SerializeField] public string ObjectName;

    /// <summary>从对象池取出复用时调用（SetActive(true) 之后）。重写以重置残留状态：刚体速度、动画、标志位等。</summary>
    public virtual void OnSpawnFromPool() { }

    /// <summary>回收进对象池时调用（SetActive(false) 前）。重写以清理订阅 / 协程 / 引用，避免带状态入池。</summary>
    public virtual void OnRecycleToPool() { }

    public virtual void PushObjectToPool()
    {
        if (ObjectName != null)
        {
            PoolManager.GetInstance().PushObj(ObjectName, gameObject);
        }
    }
    
    public virtual void PushObjectToPool(string objectName)
    {
        if (objectName != null)
        {
            PoolManager.GetInstance().PushObj(objectName, gameObject);
        }
    }
}
