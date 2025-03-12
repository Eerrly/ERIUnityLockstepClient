using System;
using UnityEngine;

/// <summary>
/// 调试控制器
/// </summary>
public class DebugTextContainer : MonoBehaviour
{
    private static DebugTextContainer _instance;

    public static DebugTextContainer Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }

    /// <summary>
    /// 获取调试文本组件
    /// </summary>
    /// <param name="owner"></param>
    /// <returns></returns>
    private DebugTextComponent GetDebugTextComponent(Transform owner)
    {
        DebugTextComponent component = null;
        var components = GetComponentsInChildren<DebugTextComponent>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].Owner == owner) component = components[i];
        }
        if (component == null)
        {
            var go = Instantiate(Resources.Load<GameObject>("Prefabs/DebugText"), transform, true);
            component = Util.GetOrAddComponent<DebugTextComponent>(go);
            component.SetOwner(owner);
        }
        return component;
    }

    /// <summary>
    /// 设置调试文本
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetText(Transform owner, string key, object value)
    {
        var component = GetDebugTextComponent(owner);
        component.SetValue(key, value);
    }

    /// <summary>
    /// 清除调试文本
    /// </summary>
    /// <param name="owner"></param>
    public void ClearText(Transform owner)
    {
        var component = GetDebugTextComponent(owner);
        component.OnRelease();
    }
    
}