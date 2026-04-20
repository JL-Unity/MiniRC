using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolObject : MonoBehaviour
{
    [SerializeField] public string ObjectName;
    
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
