using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private FrameBuffer.Input input;
    private Vector3 moveInput;
    private Vector3 lastMoveInput = Vector3.zero;
    private byte keyState;
    private List<InputKeyCode> keyCodes = new List<InputKeyCode>();

    public void AddKey(InputKeyCode keyCode)
    {
        keyCodes.Add(keyCode);
    }

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