using UnityEngine;

public class BaseView<T> : MonoBehaviour where T : BaseEntity
{
    public virtual void InitView(T entity){ }
    
    public virtual void RenderUpdate(T entity, float deltaTime){ }
    
    public virtual void OnRelease(T entity){ }
}
