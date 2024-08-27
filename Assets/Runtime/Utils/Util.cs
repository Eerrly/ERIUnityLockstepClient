using System;
using UnityEngine;

public class Util
{
    /// <summary>
    /// 获取或添加组件
    /// </summary>
    /// <param name="go"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    
    /// <summary>
    /// 执行某一个标记为该特性的所有函数
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="classType">标记了某一个特性的类特性类型</param>
    /// <param name="inherit">类继承</param>
    /// <param name="methodType">标记了某一个特性的方法特性类型</param>
    /// <param name="methodInherit">方法继承</param>
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