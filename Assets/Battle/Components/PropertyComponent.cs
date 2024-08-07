using System.IO;

public class PropertyComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public FixedNumber collisionSize;

        public Common(int no)
        {
            collisionSize = default(FixedNumber);
        }
    }

    private Common common = new Common(0);

    public FixedNumber collisionSize
    {
        get => common.collisionSize;
        set => common.collisionSize = value;
    }

    private int[] _closedPlayerEntityIds = new int[BattleSetting.MaxPlayerInRoomCount];

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
        writer.Write(_closedPlayerEntityIds.Length);
        for (int i = 0; i < _closedPlayerEntityIds.Length; i++)
            writer.Write(_closedPlayerEntityIds[i]);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.collisionSize = new FixedNumber(reader.ReadInt64());
        var _closedPlayerEntityIds_Length = reader.ReadInt32();
        for (int i = 0; i < _closedPlayerEntityIds_Length; i++)
            _closedPlayerEntityIds[i] = reader.ReadInt32();
    }
}