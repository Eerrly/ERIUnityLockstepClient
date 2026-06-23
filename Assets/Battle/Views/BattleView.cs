using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 战斗渲染类
/// </summary>
public class BattleView : BaseView<BattleEntity>
{
    /// <summary>
    /// 所有的玩家渲染列表
    /// </summary>
    private List<PlayerView> _playerViews;
    /// <summary>
    /// 文本
    /// </summary>
    private TextMesh _textMesh;
    /// <summary>
    /// 战斗HUD渲染
    /// </summary>
    private HudView _hudView;

    private float fpsDeltaTime = 0.0f;


    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <param name="entity">战斗实体</param>
    public override void InitView(BattleEntity entity)
    {
        var bv = Instantiate(Resources.Load<GameObject>(PlayerSetting.BattleViewPath), new Vector3(-11, 2.5f, 18), Quaternion.identity);
        _textMesh = Util.GetOrAddComponent<TextMesh>(bv);
        _textMesh.transform.SetParent(transform);
        
        _hudView = GetComponentInChildren<HudView>();
        _hudView.InitView();
        
        _playerViews = new List<PlayerView>();
        foreach (var playerEntity in entity.PlayerEntities)
        {
            var playerView = Util.GetOrAddComponent<PlayerView>(new GameObject($"P-{playerEntity.ID}"));
            playerView.InitView(playerEntity);
            
            if (playerEntity.ID == (GameManager.Instance.PlayerId - GameSetting.DefaultPlayerIdBase - 1)) 
                CameraManager.Instance.InitPlayerView(playerView);
            
            _playerViews.Add(playerView);
        }
    }

    /// <summary>
    /// 渲染轮询
    /// </summary>
    /// <param name="entity">战斗实体</param>
    /// <param name="deltaTime">增量时间</param>
    public override void RenderUpdate(BattleEntity entity, float deltaTime)
    {
        fpsDeltaTime += (Time.unscaledDeltaTime - fpsDeltaTime) * 0.1f;
        
        if (entity != null && _textMesh != null)
            _textMesh.text = $"Name:{System.Enum.GetName(typeof(EBattleEntityType), entity.BattleEntityType)} Frame:{entity.Frame} Time:{entity.Time.ToString()}\nFPS:{(1.0f / fpsDeltaTime):0.}";

        if(_playerViews == null || _playerViews.Count != entity.PlayerEntities.Count) return;
        
        foreach (var playerEntity in entity.PlayerEntities)
        {
            foreach (var t in _playerViews.Where(t => t.ID == playerEntity.ID))
            {
                t.RenderUpdate(playerEntity, deltaTime);
                t.AfterRenderUpdate(playerEntity);
            }
        }
        
        _hudView.RenderUpdate(entity, this);
    }

    /// <summary>
    /// 释放
    /// </summary>
    /// <param name="entity"></param>
    public override void OnRelease(BattleEntity entity)
    {
        if (_playerViews == null)
            return;

        foreach (var playerView in _playerViews)
        {
            if (playerView == null)
                continue;

            if (Application.isPlaying)
                Destroy(playerView.gameObject);
            else
                DestroyImmediate(playerView.gameObject);
        }

        _playerViews.Clear();
    }

    /// <summary>
    /// 获取玩家渲染
    /// </summary>
    /// <param name="id">玩家ID</param>
    /// <returns>玩家渲染实例</returns>
    public PlayerView GetPlayerView(int id)
    {
        return _playerViews.FirstOrDefault(t => t.ID == id);
    }
    
}
