using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 对象池】
/// 需要把预制体放在文件夹Resoures中（几级目录都行，但是层级越少越好）
/// </summary>
public class PoolManager : BaseManager<PoolManager>
{
    public Dictionary<string, List<GameObject>> poolDic = new Dictionary<string, List<GameObject>>();
    /// <summary>
    /// 从对象池中取得物体
    /// </summary>
    /// <param name="name">物品名称</param>
    /// <returns></returns>
    public GameObject GetObject(string name, Vector3 location)
    {
        GameObject obj = null;
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            //LogClass.LogGame(GameLogCategory.System, "Find item" + name);
            obj = poolDic[name][0];
            poolDic[name].RemoveAt(0);
        }
        else
        {
            GameObject loadObject = Resources.Load<GameObject>(name);
            if(loadObject) 
            {
                obj = GameObject.Instantiate(loadObject, location, Quaternion.identity); 
                //LogClass.LogGame(GameLogCategory.System, "Create item" + name);
            }
        }
        // 资源路径错误 / 资源不在 Resources 下，或池里残留的对象已被销毁（fake-null）：返回 null 让上层降级，不再无脑 SetActive 触发 NRE
        if (obj == null)
        {
            LogClass.LogWarning(GameLogCategory.System, "PoolManager.GetObject: 取对象失败，path=" + name);
            return null;
        }
        obj.transform.position = location;
        obj.SetActive(true);
        NotifySpawn(obj);
        return obj;
    }
    
    public GameObject GetObject(string name, Vector3 location, Quaternion rotation)
    {
        GameObject obj = null;
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            //LogClass.LogGame(GameLogCategory.System, "Find item" + name);
            obj = poolDic[name][0];
            poolDic[name].RemoveAt(0);
        }
        else
        {
            GameObject loadObject = Resources.Load<GameObject>(name);
            if(loadObject) 
            {
                obj = GameObject.Instantiate(loadObject, location, rotation); 
                //LogClass.LogGame(GameLogCategory.System, "Create item" + name);
            }
        }
        if (obj == null)
        {
            LogClass.LogWarning(GameLogCategory.System, "PoolManager.GetObject: 取对象失败，path=" + name);
            return null;
        }
        obj.transform.SetPositionAndRotation(location, rotation);
        obj.SetActive(true);
        NotifySpawn(obj);
        return obj;
    }
    
    /// <summary>
    /// UI生成使用该函数
    /// </summary>
    /// <returns></returns>
    public GameObject GetUIObject(string name, Transform parentTransfrom)
    {
        GameObject obj = null;
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            //LogClass.LogGame(GameLogCategory.System, "Find UI:" + name);
            obj = poolDic[name][0];
            poolDic[name].RemoveAt(0);
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>(name);
            if (prefab != null)
            {
                obj = GameObject.Instantiate(prefab, parentTransfrom, false);
            }
            //LogClass.LogGame(GameLogCategory.System, name);
        }
        // 面板预制体路径错误，或池里残留对象已随旧 Canvas 销毁：返回 null 让 UIManager 打 warning 兜底
        if (obj == null)
        {
            LogClass.LogWarning(GameLogCategory.UIManager, "PoolManager.GetUIObject: 取对象失败，path=" + name);
            return null;
        }
        obj.SetActive(true);
        NotifySpawn(obj);
        return obj;
    }
    public void PushObj(string name, GameObject obj)
    {
        if (obj == null)
        {
            return;
        }
        // 回收前给业务一个清理状态的机会（清订阅/协程/速度等），再失活入池
        if (obj.TryGetComponent<PoolObject>(out var poolObject))
        {
            poolObject.OnRecycleToPool();
        }
        obj.SetActive(false);
        if (poolDic.ContainsKey(name))
        {
            poolDic[name].Add(obj);
        }
        else
        {
            poolDic.Add(name, new List<GameObject>() { obj });
        }
    }
    /// <summary>取出复用后通知业务重置状态（挂了 PoolObject 才有钩子）。</summary>
    private static void NotifySpawn(GameObject obj)
    {
        if (obj.TryGetComponent<PoolObject>(out var poolObject))
        {
            poolObject.OnSpawnFromPool();
        }
    }

    /// <summary>
    /// 场景切换时调用
    /// </summary>
    public override void Clear()
    {
        poolDic.Clear();
        LogClass.LogGame(GameLogCategory.System, "PoolManager: 对象池清空");
    }
}