using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗HUD视图
/// </summary>
public class HudView : MonoBehaviour
{
    
    /// <summary>
    /// HUD节点配置（每个玩家独立的HUD元素）
    /// </summary>
    [System.Serializable]
    public class Node
    {
        public Transform hudParent;
        public Slider hpSlider;
    }

    public List<Node> Nodes;
    /// <summary>
    /// HUD相对于角色位置的偏移量
    /// </summary>
    public Vector3 Offset;

    /// <summary>
    /// 初始化视图，设置画布渲染相机
    /// </summary>
    public void InitView()
    {
        CameraManager.Instance.SetCanvasBattleCamera(gameObject);
    }

    public void RenderUpdate(BattleEntity battleEntity, BattleView battleView)
    {
        var playerEntities = battleEntity.PlayerEntities;
        for (var i = 0; i < playerEntities.Count; i++)
        {
            var node = Nodes[i];
            var playerEntity = playerEntities[i];
            var playerView = battleView.GetPlayerView(playerEntity.ID);

            var pos = playerView.transform.position;
            node.hudParent.position = pos + Offset;

            node.hpSlider.value = Mathf.Clamp01((float)playerEntity.Property.hp / PlayerInitPropertyConstants.TotalHp);
        }
        
    }



}