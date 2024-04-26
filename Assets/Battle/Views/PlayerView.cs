using UnityEngine;

public class PlayerView : BaseView<PlayerEntity>
{
    public int ID;
    
    private Vector3 fixV;
    
    public override void InitView(PlayerEntity entity)
    {
        ID = entity.ID;
        var character = Instantiate(Resources.Load<GameObject>(BattleSetting.PlayerCharacterPath), Vector3.zero, Quaternion.identity);
        character.transform.SetParent(transform, false);
        var meshRenders = character.GetComponentsInChildren<MeshRenderer>();
        foreach (var render in meshRenders)
            render.material.color = BattleSetting.InitPlayerColor[entity.ID];
    }

    public override void RenderUpdate(PlayerEntity entity, float deltaTime)
    {
        TransformUpdate(entity, deltaTime);
    }

    private void TransformUpdate(PlayerEntity entity, float deltaTime)
    {
        var currentPosition = transform.position;
        var nextDeltaPosition = currentPosition + entity.Movement.position.ToVector3();
        if((currentPosition - nextDeltaPosition).sqrMagnitude >= 4)
        {
            currentPosition = Vector3.Lerp(currentPosition, nextDeltaPosition, deltaTime);
        }
        var currentRotation = transform.rotation;
        var nextDeltaRotation = entity.Movement.rotation.ToQuaternion();
        if(currentRotation != nextDeltaRotation)
        {
            var forward = MoveSystem.GetForwardAngle(entity).ToFloat();
            var target = MoveSystem.GetTargetAngle(entity).ToFloat();
            var angle = Mathf.MoveTowardsAngle(forward, target, entity.Movement.turnSpeed.ToFloat() * deltaTime);
            currentRotation = Quaternion.Euler(0f, angle, 0f);
        }

        var offset = entity.Transform.pos.ToVector3() - currentPosition;
        var dis = 0.4f;
        if (offset.magnitude > dis)
        {
            var target = currentPosition + offset.normalized * (offset.magnitude - dis);
            currentPosition = Vector3.SmoothDamp(currentPosition, target, ref fixV, 0.2f);
        }
        else
        {
            fixV = Vector3.zero;
        }

        var transform1 = transform;
        transform1.position = currentPosition;
        transform1.rotation = currentRotation;
    }
}