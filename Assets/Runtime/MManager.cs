using UnityEngine;

public abstract class MManager<T> : MonoBehaviour, IManager where T : Component
{
    private static T _instance;
    
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
    
    public virtual void Initialize() { }
    
    public virtual void OnRelease() { }
}