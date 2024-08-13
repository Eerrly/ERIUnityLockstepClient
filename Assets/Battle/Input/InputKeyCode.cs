/// <summary>
/// 输入按键
/// </summary>
public class InputKeyCode
{
    /// <summary>
    /// 开关
    /// </summary>
    private bool enable = true;

    /// <summary>
    /// 键位名称
    /// </summary>
    public string Name;
    public bool State => enable && InputManager.Instance.GetKeyCode(Name);

    /// <summary>
    /// 设置开关
    /// </summary>
    /// <param name="enable">开关</param>
    public void SetEnable(bool enable)
    {
        this.enable = enable;
    }
}