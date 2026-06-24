using System;
using System.Collections.Generic;
using NUnit.Framework;

public class BattleEntitySystemLifecycleControllerTests
{
    [Test]
    public void InitializeInvokesEntitySystemInitialize()
    {
        var calls = new List<Type>();
        var controller = new BattleEntitySystemLifecycleController(methodType => calls.Add(methodType));

        controller.Initialize();

        CollectionAssert.AreEqual(new[] { typeof(EntitySystem.Initialize) }, calls);
    }

    [Test]
    public void ReleaseInvokesEntitySystemRelease()
    {
        var calls = new List<Type>();
        var controller = new BattleEntitySystemLifecycleController(methodType => calls.Add(methodType));

        controller.Release();

        CollectionAssert.AreEqual(new[] { typeof(EntitySystem.Release) }, calls);
    }

    [Test]
    public void InitializeThenReleaseKeepsLifecycleOrder()
    {
        var calls = new List<Type>();
        var controller = new BattleEntitySystemLifecycleController(methodType => calls.Add(methodType));

        controller.Initialize();
        controller.Release();

        CollectionAssert.AreEqual(
            new[] { typeof(EntitySystem.Initialize), typeof(EntitySystem.Release) },
            calls);
    }
}
