using NUnit.Framework;

public class BattleViewLifecycleControllerTests
{
    [Test]
    public void EnsureViewCreatesBattleViewAndBindsIt()
    {
        BattleView boundView = null;
        var controller = new BattleViewLifecycleController(view => boundView = view);

        var view = controller.EnsureView();

        Assert.IsNotNull(view);
        Assert.AreEqual(view, controller.View);
        Assert.AreEqual(view, boundView);
        Assert.IsTrue(controller.HasView);

        controller.ReleaseView(null);
    }

    [Test]
    public void RecreateViewDestroysOldViewAndBindsNewView()
    {
        BattleView boundView = null;
        var controller = new BattleViewLifecycleController(view => boundView = view);
        var oldView = controller.EnsureView();

        var newView = controller.RecreateView();

        Assert.IsNotNull(newView);
        Assert.AreNotEqual(oldView, newView);
        Assert.AreEqual(newView, controller.View);
        Assert.AreEqual(newView, boundView);

        controller.ReleaseView(null);
    }

    [Test]
    public void ReleaseViewClearsViewAndBinding()
    {
        BattleView boundView = null;
        var controller = new BattleViewLifecycleController(view => boundView = view);
        controller.EnsureView();

        controller.ReleaseView(null);

        Assert.IsNull(controller.View);
        Assert.IsNull(boundView);
        Assert.IsFalse(controller.HasView);
        Assert.IsFalse(controller.IsInitialized);
    }
}
