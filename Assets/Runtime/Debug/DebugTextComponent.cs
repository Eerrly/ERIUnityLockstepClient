using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 调试文本组件
/// </summary>
[RequireComponent(typeof(Text))]
public class DebugTextComponent : MonoBehaviour
{
    private Text _text;
    private Dictionary<string, object> _valueDic = new Dictionary<string, object>();
    private StringBuilder _sb = new StringBuilder();
    
    /// <summary>
    /// 所属物体
    /// </summary>
    public Transform Owner { get; private set; }

    private void Start()
    {
        _text = Util.GetOrAddComponent<Text>(gameObject);
        _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void Update()
    {
        if (Owner != null)
        {
            if (Camera.main == null) return;
            var pos = Camera.main.WorldToScreenPoint(Owner.position);
            transform.position = pos;

            _sb.Remove(0, _sb.Length);
            using (var iterator = _valueDic.GetEnumerator())
            {
                while (iterator.MoveNext())
                    _sb.AppendFormat("{0}:{1}\n", iterator.Current.Key, iterator.Current.Value);
            }
            _text.text = _sb.ToString();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 设置需要显示的文本
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void SetValue(string key, object value)
    {
        _valueDic[key] = value;
    }

    /// <summary>
    /// 设置拥有者
    /// </summary>
    /// <param name="owner"></param>
    public void SetOwner(Transform owner)
    {
        Owner = owner;
    }

    public void OnRelease()
    {
        _valueDic.Clear();
    }

}