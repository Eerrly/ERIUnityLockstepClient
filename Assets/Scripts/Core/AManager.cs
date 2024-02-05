public abstract class AManager<T> : IManager where T:new()
{
    private static T _instance;

    /// <summary>
    /// 静态单例
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new T();
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// 初始化
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// 释放
    /// </summary>
    public virtual void OnRelease() { }
    
}