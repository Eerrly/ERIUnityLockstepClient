using System;

public class BattleEntitySystemLifecycleController
{
    private readonly Action<Type> _invokeEntitySystemMethod;

    public BattleEntitySystemLifecycleController(object attributeHost)
        : this(methodType => Util.InvokeAttributeCall(
            attributeHost,
            typeof(EntitySystem),
            false,
            methodType,
            false))
    {
    }

    public BattleEntitySystemLifecycleController(Action<Type> invokeEntitySystemMethod)
    {
        _invokeEntitySystemMethod = invokeEntitySystemMethod ?? throw new ArgumentNullException(nameof(invokeEntitySystemMethod));
    }

    public void Initialize()
    {
        _invokeEntitySystemMethod(typeof(EntitySystem.Initialize));
    }

    public void Release()
    {
        _invokeEntitySystemMethod(typeof(EntitySystem.Release));
    }
}
