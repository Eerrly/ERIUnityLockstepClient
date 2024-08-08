using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleView : BaseView<BattleEntity>
{
    public string Name;
    private List<PlayerView> _playerViews;

    private TextMesh _textMesh;

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
            _playerViews.Add(playerView);
        }
    }

    public override void RenderUpdate(BattleEntity entity, float deltaTime)
    {
        if (entity != null && _textMesh != null)
            _textMesh.text = $"BattleEntity Name:{entity.Name} Time:{entity.Time.ToString()}";

        if(_playerViews == null || _playerViews.Count != entity.PlayerEntities.Count) return;
        
        foreach (var playerEntity in entity.PlayerEntities)
        {
            foreach (var t in _playerViews.Where(t => t.ID == playerEntity.ID))
                t.RenderUpdate(playerEntity, deltaTime);
        }
    }

    public override void OnRelease(BattleEntity entity)
    {
        foreach (var playerView in _playerViews)
            DestroyImmediate(playerView.gameObject);
        _playerViews.Clear();
    }
}