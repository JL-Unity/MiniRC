public class BaseManager<T> where T : new()
{
    private static T instance;
    public static T GetInstance()
    {
        if (instance == null)
        {
            instance = new T();
            return instance;
        }
        else
        {
            return instance;
        }
    }

    /// <summary>
    /// 复位静态实例。用于关闭 Domain Reload 时，进入 Play 前清掉上一会话残留的脏单例。
    /// 由 <see cref="FrameworkBootstrap"/> 在 SubsystemRegistration 阶段统一调用。
    /// </summary>
    public static void ResetInstance()
    {
        instance = default;
    }

    public virtual void Init() { }

    public virtual void Update() { }

    public virtual void Clear() { }
}