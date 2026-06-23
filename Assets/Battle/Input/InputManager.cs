using UnityEngine;

/// <summary>
/// 输入管理器
/// </summary>
public class InputManager : MManager<InputManager>
{
    /// <summary>
    /// 开关
    /// </summary>
    public bool Enabled;
    /// <summary>
    /// 默认按键状态
    /// </summary>
    public bool[] DefaultKeies;

    /// <summary>
    /// 玩家输入
    /// </summary>
    private PlayerInput playerInput;

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        playerInput = Util.GetOrAddComponent<PlayerInput>(gameObject);
        Enabled = true;
        DefaultKeies = new[] { false, false, false };
        
        playerInput.AddKey(new InputKeyCode(){ Name = InputSetting.KeyCodeJ });
        playerInput.AddKey(new InputKeyCode(){ Name = InputSetting.KeyCodeK });
        playerInput.AddKey(new InputKeyCode(){ Name = InputSetting.KeyCodeL });
    }

    /// <summary>
    /// 纵轴输入
    /// </summary>
    /// <returns>纵轴输入值</returns>
    public float Vertical()
    {
        return !Enabled ? 0f : Input.GetAxisRaw(InputSetting.Vertical);
    }

    /// <summary>
    /// 横轴输入
    /// </summary>
    /// <returns>横轴输入值</returns>
    public float Horizontal()
    {
        return !Enabled ? 0f : Input.GetAxisRaw(InputSetting.Horizontal);
    }

    /// <summary>
    /// 获取某一个按键的状态
    /// </summary>
    /// <param name="keyName">键位名称</param>
    /// <returns>按键状态</returns>
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

    /// <summary>
    /// 重置按键状态
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < DefaultKeies.Length; i++)
            DefaultKeies[i] = false;
    }

    /// <summary>
    /// 获取某一个战斗POS当前的操作数据
    /// </summary>
    /// <param name="pos">战斗POS</param>
    /// <returns>操作数据</returns>
    public FrameBuffer.Input GetInput(int pos)
    {
        if (!TryGetInput(pos, out var input))
            return new FrameBuffer.Input(byte.MaxValue);

        return input;
    }

    /// <summary>
    /// 尝试获取当前输入，不触发任何单例或 GameObject 创建
    /// </summary>
    public bool TryGetInput(int pos, out FrameBuffer.Input input)
    {
        input = new FrameBuffer.Input(byte.MaxValue);
        if (playerInput == null)
            return false;

        input = playerInput.GetPlayerInput(pos);
        return true;
    }

    /// <summary>
    /// 释放
    /// </summary>
    public override void OnRelease()
    {
        Reset();
    }

}
