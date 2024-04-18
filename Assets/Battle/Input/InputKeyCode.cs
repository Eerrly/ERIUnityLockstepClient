public class InputKeyCode
{
    private bool enable = true;

    public string Name;
    public bool State => enable && InputManager.Instance.GetKeyCode(Name);

    public void SetEnable(bool enable)
    {
        this.enable = enable;
    }
}