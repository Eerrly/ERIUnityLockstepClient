using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家输入
/// </summary>
public class PlayerInput : MonoBehaviour
{
    /// <summary>
    /// 操作数据
    /// </summary>
    private FrameBuffer.Input input;
    /// <summary>
    /// 纵横位移向量
    /// </summary>
    private Vector3 moveInput;
    /// <summary>
    /// 上次纵横位移向量
    /// </summary>
    private Vector3 lastMoveInput = Vector3.zero;
    /// <summary>
    /// 按键状态
    /// </summary>
    private byte keyState;
    /// <summary>
    /// 输入按键列表
    /// </summary>
    private List<InputKeyCode> keyCodes = new List<InputKeyCode>();

    /// <summary>
    /// 添加输入按键
    /// </summary>
    /// <param name="keyCode"></param>
    public void AddKey(InputKeyCode keyCode)
    {
        keyCodes.Add(keyCode);
    }

    /// <summary>
    /// 通过玩家战斗POS获取操作数据
    /// </summary>
    /// <param name="pos">战斗POS</param>
    /// <returns>操作数据</returns>
    public FrameBuffer.Input GetPlayerInput(int pos)
    {
        input = new FrameBuffer.Input
        {
            pos = (byte)pos,
            yaw = (byte)FixedMath.Format8DirInput(new FixedVector3(moveInput)),
            key = keyState
        };
        return input;
    }

    /// <summary>
    /// 轮询
    /// </summary>
    private void Update()
    {
        var tmpKeyState = (byte)0;
        for (var i = 0; i < keyCodes.Count; ++i)
        {
            if (!keyCodes[i].State) continue;
            tmpKeyState |= (byte)(1 << i);
            break;
        }
        keyState = tmpKeyState;

        var tmpMoveInput = (InputManager.Instance.Vertical() * Vector3.forward) + (InputManager.Instance.Horizontal() * Vector3.right);
        if(lastMoveInput != Vector3.zero && tmpMoveInput == Vector3.zero)
            moveInput = lastMoveInput;
        else
            moveInput = tmpMoveInput.normalized;
        lastMoveInput = tmpMoveInput.normalized;
    }
}