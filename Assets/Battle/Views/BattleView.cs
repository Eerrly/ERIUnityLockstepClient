using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 战斗渲染类
/// </summary>
public class BattleView : BaseView<BattleEntity>
{
    /// <summary>
    /// 战斗实体名称
    /// </summary>
    public string Name;
    /// <summary>
    /// 所有的玩家渲染列表
    /// </summary>
    private List<PlayerView> _playerViews;

    private TextMesh _textMesh;

    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <param name="entity">战斗实体</param>
    public override void InitView(BattleEntity entity)
    {
        Name = entity.Name;
        var bv = Instantiate(Resources.Load<GameObject>(BattleSetting.BattleViewPath), new Vector3(-12, 0, 0), Quaternion.identity);
        _textMesh = Util.GetOrAddComponent<TextMesh>(bv);
        _textMesh.transform.SetParent(transform);
        
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
        if (entity != null && _textMesh != null)
            _textMesh.text = $"Name:{entity.Name} Frame:{entity.Frame} Time:{entity.Time.ToString()}";

        if(_playerViews == null || _playerViews.Count != entity.PlayerEntities.Count) return;
        
        foreach (var playerEntity in entity.PlayerEntities)
        {
            foreach (var t in _playerViews.Where(t => t.ID == playerEntity.ID))
                t.RenderUpdate(playerEntity, deltaTime);
        }
    }

    /// <summary>
    /// 释放
    /// </summary>
    /// <param name="entity"></param>
    public override void OnRelease(BattleEntity entity)
    {
        foreach (var playerView in _playerViews)
            DestroyImmediate(playerView.gameObject);
        _playerViews.Clear();
    }
}