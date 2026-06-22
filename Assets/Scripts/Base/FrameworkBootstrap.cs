using UnityEngine;

/// <summary>
/// 关闭 Domain Reload（Project Settings → Editor → Enter Play Mode Options）时，
/// <see cref="BaseManager{T}"/> 的静态实例会跨 Play 会话残留上一局的脏状态（字典、订阅等）。
/// 这里在每次进入 Play 的最早时机 SubsystemRegistration（先于任何场景 Awake）显式复位所有已知管理器。
/// 泛型基类的静态字段按封闭类型各自独立，单个特性无法自动覆盖，故维护一份显式列表；新增管理器时在此补一行。
/// </summary>
public static class FrameworkBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetManagers()
    {
        BaseManager<EventCenter>.ResetInstance();
        BaseManager<PoolManager>.ResetInstance();
        BaseManager<UIManager>.ResetInstance();
        BaseManager<TimerManager>.ResetInstance();
        BaseManager<SkillManager>.ResetInstance();
        BaseManager<AudioManager>.ResetInstance();
        BaseManager<UiColorService>.ResetInstance();
    }
}
