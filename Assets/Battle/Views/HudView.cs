using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudView : MonoBehaviour
{

    [System.Serializable]
    public class Node
    {
        public Transform hudParent;
        public Slider hpSlider;
    }

    public List<Node> Nodes;
    public Vector3 Offset;

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