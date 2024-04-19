using System;
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
    
    public static void InvokeAttributeCall(object obj, Type classType, bool inherit, Type methodType, bool methodInherit)
    {
        if (null == obj) return;
        
        var types = obj.GetType().Assembly.GetExportedTypes();
        foreach (var t in types)
        {
            if (!t.IsDefined(classType, inherit)) continue;
            
            var methods = t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var t1 in methods)
            {
                if (t1.IsDefined(methodType, methodInherit))
                {
                    t1.Invoke(null, null);
                }
            }
        }
    }
    
}