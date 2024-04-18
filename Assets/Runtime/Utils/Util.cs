using UnityEngine;

public class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T c = null;
        if (null != go)
        {
            c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
            }
        }
        return c;
    }
}