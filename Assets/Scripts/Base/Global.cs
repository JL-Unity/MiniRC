using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Resources 下路径常量（拼写已修正，便于检索）。</summary>
public static class ResourcePath
{
    public const string RoadPath = "Road/";
}

public static class PanelPath
{
    public const string StartUpPath = "UI/StartUp/";
    public const string OperationPath = "UI/Operation/";
}

/// <summary>游戏内日志分类字符串，避免与 UnityEngine.LogType 混淆。</summary>
public static class GameLogCategory
{
    public const string PlayerState = "PlayerState";
    public const string UIManager = "UIManger";
    public const string SceneStateController = "SceneStateController";
    public const string System = "System";
    public const string RcCar = "RcCar";
}

public static class LogClass
{
    public static void LogGame(string category, object logInfo)
    {
#if UNITY_EDITOR
        Debug.Log("【" + category + "】:" + logInfo);
#endif
    }
    
    public static void LogImport(string category, object logInfo)
    {
        Debug.Log("【" + category + "】:" + logInfo);
    }
    
    public static void LogError(string category, object logInfo)
    {
        Debug.LogError("【" + category + "】:" + logInfo);
    }
    
    public static void LogWarning(string category, object logInfo)
    {
#if UNITY_EDITOR
        Debug.LogWarning("【" + category + "】:" + logInfo);
#endif
    }
}