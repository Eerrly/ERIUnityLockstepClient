using UnityEngine;

/// <summary>
/// 渲染类基类
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public class BaseView<T> : MonoBehaviour where T : BaseEntity
{
    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <param name="entity">实体</param>
    public virtual void InitView(T entity){ }
    
    /// <summary>
    /// 渲染轮询
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="deltaTime">增量时间</param>
    public virtual void RenderUpdate(T entity, float deltaTime){ }
    
    /// <summary>
    /// 渲染轮询
    /// </summary>
    /// <param name="entity"></param>
    public virtual void AfterRenderUpdate(T entity){ }
    
    /// <summary>
    /// 释放
    /// </summary>
    /// <param name="entity">实体</param>
    public virtual void OnRelease(T entity){ }
}
