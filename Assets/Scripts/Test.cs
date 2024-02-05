using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private int f = 16;
    
    void Start()
    {

        var a = toN(7, 10);
        var b = toN(13, 10);
        Debug.Log( "a , doubule值：" + todouble(a).ToString("f4") + ", float值：" + tofloat(a).ToString("f4"));
        Debug.Log( "b , doubule值：" + todouble(b).ToString("f4") + ", float值：" + tofloat(b).ToString("f4"));

        var c = division(a, b);
        Debug.Log( "c , doubule值：" + todouble(c).ToString("f4") + ", float值：" + tofloat(c).ToString("f4"));
    }

    long toN(long x, long y)
    {
        return ((x << (f + 1)) / y + 1) >> 1;
    }

    long division(long x, long y)
    {
        // 模拟浮点数除法
        return (x << f) / y;
    }

    double todouble(long n)
    {
        // 获取掩码
        var mask = (1 << f) - 1;
        // 整数部分
        var a = n >> f;
        // 小数部分
        var b = n & mask;
        // 小数位
        var c = mask + 1;
        // 拿到小数值
        var d = b / (double)c;
        // 整数 + 小数
        var e = a + d;
        return e;
    }

    float tofloat(long n)
    {
        var mask = (1 << f) - 1;
        var a = n >> f;
        var b = n & mask;
        var c = mask + 1;
        var d = b / (float)c;
        var e = a + d;
        return e;
    }
    
}
