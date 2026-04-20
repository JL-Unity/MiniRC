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
            //Debug.Log("Find item" + name);
            obj = poolDic[name][0];
            obj.transform.position = location;
            poolDic[name].RemoveAt(0);
        }
        else
        {
            GameObject loadObject = Resources.Load<GameObject>(name);
            if(loadObject) 
            {
                obj = GameObject.Instantiate(loadObject, location, Quaternion.identity); 
                //Debug.Log("Create item" + name);
            }
        }
        obj.SetActive(true);
        return obj;
    }
    
    public GameObject GetObject(string name, Vector3 location, Quaternion rotation)
    {
        GameObject obj = null;
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            //Debug.Log("Find item" + name);
            obj = poolDic[name][0];
            obj.transform.position = location;
            poolDic[name].RemoveAt(0);
        }
        else
        {
            GameObject loadObject = Resources.Load<GameObject>(name);
            if(loadObject) 
            {
                obj = GameObject.Instantiate(loadObject, location, rotation); 
                //Debug.Log("Create item" + name);
            }
        }
        obj.SetActive(true);
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
            //Debug.Log("Find UI:" + name);
            obj = poolDic[name][0];
            poolDic[name].RemoveAt(0);
        }
        else
        {
            obj = GameObject.Instantiate(Resources.Load<GameObject>(name), parentTransfrom, false);
            //Debug.Log(name);
        }
        obj.SetActive(true);
        return obj;
    }
    public void PushObj(string name, GameObject obj)
    {
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
    /// <summary>
    /// 场景切换时调用
    /// </summary>
    public override void Clear()
    {
        poolDic.Clear();
        Debug.Log("【PoolManager】对象池清空");
    }
}