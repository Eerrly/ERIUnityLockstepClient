using UnityEngine;

/// <summary>
/// 继承Mono的单例管理器父类
/// </summary>
/// <typeparam name="T">管理器类型</typeparam>
public abstract class MManager<T> : MonoBehaviour, IManager where T : Component
{
    private static T _instance;
    
    /// <summary>
    /// 单例
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Util.GetOrAddComponent<T>(new GameObject(typeof(T).FullName));
                DontDestroyOnLoad(_instance);
            }
            return _instance;
        }
    }

    /// <summary>
    /// 单例是否已存在，不触发创建
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// 获取已存在的单例，不触发创建
    /// </summary>
    public static T InstanceOrNull => _instance;
    
    /// <summary>
    /// 初始化
    /// </summary>
    public virtual void Initialize() { }
    
    /// <summary>
    /// 释放
    /// </summary>
    public virtual void OnRelease() { }
}
