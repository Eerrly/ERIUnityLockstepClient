using System.IO;

/// <summary>
/// 属性组件
/// </summary>
public class PropertyComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public FixedNumber collisionSize;
        public FixedNumber attackDistance;
        public FixedNumber attackAngle;
        public System.Int32 effect;

        public Common(int no)
        {
            collisionSize = default(FixedNumber);
            attackDistance = default(FixedNumber);
            attackAngle = default(FixedNumber);
            effect = default(System.Int32);
        }
    }

    private Common common = new Common(0);
    
    /// <summary>
    /// 碰撞半径
    /// </summary>
    public FixedNumber collisionSize
    {
        get => common.collisionSize;
        set => common.collisionSize = value;
    }

    /// <summary>
    /// 攻击距离
    /// </summary>
    public FixedNumber attackDistance
    {
        get => common.attackDistance;
        set => common.attackDistance = value;
    }

    /// <summary>
    /// 攻击扇形角度
    /// </summary>
    public FixedNumber attackAngle
    {
        get => common.attackAngle;
        set => common.attackAngle = value;
    }

    public System.Int32 effect
    {
        get => common.effect;
        set => common.effect = value;
    }

    private int[] _closedPlayerEntityIds = new int[BattleSetting.MaxPlayerInRoomCount];
    /// <summary>
    /// 与其有碰撞的其他玩家实体
    /// </summary>
    public int[] closedPlayerEntityIds
    {
        get => _closedPlayerEntityIds;
        set => _closedPlayerEntityIds = value;
    }

    public override void CopyTo(BaseComponent component)
    {
        var data = component as PropertyComponent;
        data.common = common;
        System.Array.Copy(_closedPlayerEntityIds, data._closedPlayerEntityIds, _closedPlayerEntityIds.Length);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(common.collisionSize._raw);
        writer.Write(common.attackDistance._raw);
        writer.Write(common.attackAngle._raw);
        writer.Write(common.effect);
        writer.Write(_closedPlayerEntityIds.Length);
        for (int i = 0; i < _closedPlayerEntityIds.Length; i++)
            writer.Write(_closedPlayerEntityIds[i]);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.collisionSize = new FixedNumber(reader.ReadInt64());
        common.attackDistance = new FixedNumber(reader.ReadInt64());
        common.attackAngle = new FixedNumber(reader.ReadInt64());
        common.effect = reader.ReadInt32();
        var _closedPlayerEntityIds_Length = reader.ReadInt32();
        for (int i = 0; i < _closedPlayerEntityIds_Length; i++)
            _closedPlayerEntityIds[i] = reader.ReadInt32();
    }
}