using System;
using UnityEngine;

/// <summary>
/// 管理 BattleView 的创建、初始化、渲染转发和释放。
/// </summary>
public class BattleViewLifecycleController
{
    private readonly Action<BattleView> _bindBattleView;

    public BattleView View { get; private set; }
    public bool HasView => View != null;
    public bool IsInitialized { get; private set; }

    public BattleViewLifecycleController(Action<BattleView> bindBattleView)
    {
        _bindBattleView = bindBattleView;
    }

    public BattleView EnsureView()
    {
        return View == null ? RecreateView() : View;
    }

    public BattleView RecreateView()
    {
        DestroyViewObject();

        var go = new GameObject("BattleView");
        View = Util.GetOrAddComponent<BattleView>(go);
        IsInitialized = false;
        _bindBattleView?.Invoke(View);
        return View;
    }

    public void InitView(BattleEntity entity)
    {
        EnsureView().InitView(entity);
        IsInitialized = true;
    }

    public void RenderUpdate(BattleEntity entity, float deltaTime)
    {
        if (View == null)
            return;

        View.RenderUpdate(entity, deltaTime);
    }

    public void ReleaseView(BattleEntity entity)
    {
        if (View == null)
            return;

        if (IsInitialized)
            View.OnRelease(entity);

        DestroyViewObject();
    }

    private void DestroyViewObject()
    {
        if (View != null)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(View.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(View.gameObject);
        }

        View = null;
        IsInitialized = false;
        _bindBattleView?.Invoke(null);
    }
}
