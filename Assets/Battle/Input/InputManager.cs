using UnityEngine;

public class InputManager : MManager<InputManager>
{
    public bool Enabled;
    public bool[] DefaultKeies;

    private PlayerInput playerInput;

    public override void Initialize()
    {
        playerInput = Util.GetOrAddComponent<PlayerInput>(gameObject);
        Enabled = true;
        DefaultKeies = new[] { false, false, false };
        
        playerInput.AddKey(new InputKeyCode(){ Name = InputSetting.KeyCodeJ });
        playerInput.AddKey(new InputKeyCode(){ Name = InputSetting.KeyCodeK });
        playerInput.AddKey(new InputKeyCode(){ Name = InputSetting.KeyCodeL });
    }

    public float Vertical()
    {
        return !Enabled ? 0f : Input.GetAxisRaw(InputSetting.Vertical);
    }

    public float Horizontal()
    {
        return !Enabled ? 0f : Input.GetAxisRaw(InputSetting.Horizontal);
    }

    public bool GetKeyCode(string keyName)
    {
        if (!Enabled) return false;
        
        var state = Input.GetKey(keyName);
        if (!state)
        {
            switch (keyName)
            {
                case InputSetting.KeyCodeJ:
                    state = DefaultKeies[0];
                    break;
                case InputSetting.KeyCodeK:
                    state = DefaultKeies[1];
                    break;
                case InputSetting.KeyCodeL:
                    state = DefaultKeies[2];
                    break;
            }
        }
        return state;
    }

    public void Reset()
    {
        for (int i = 0; i < DefaultKeies.Length; i++)
            DefaultKeies[i] = false;
    }

    public FrameBuffer.Input GetInput(int pos)
    {
        var input = playerInput.GetPlayerInput(pos);
        return input;
    }

    public override void OnRelease()
    {
        Reset();
    }

}