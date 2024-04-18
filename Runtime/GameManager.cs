using System.Runtime.Versioning;

public class GameManager : AManager<GameManager>
{
    public uint PlayerId;
    public RoomInfo RoomInfo;
    public byte[] Input;
    public int ServerAuthorityFrame = -1;
    public bool IsBattleConnected = false;
    public bool IsBattleStart = false;

    private FrameBuffer frameBuffer;
    public FrameBuffer FrameBuffer
    {
        get => frameBuffer;
        set => frameBuffer = value;
    }

    private FrameEngine frameEngine;
    private BattleController battleController;


    public override void Initialize()
    {
        Input = new byte[BattleSetting.MaxFrameCount];

        battleController = new BattleController();
        frameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount);
        frameEngine = new FrameEngine();
        frameEngine.RegisterNetUpdateListener(battleController.NetUpdate);
        frameEngine.RegisterFrameUpdateListener(battleController.LogicUpdate);
    }

    public void StartBattle()
    {
        battleController.InitEntities();
        frameEngine.StartNetEngine(BattleSetting.NetInterval);
        frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
    }

    public int GetBattlePos()
    {
        return (int)(PlayerId - GameSetting.DefaultPlayerIdBase - 1);
    }

    public void SetFrame(byte data)
    {
        Input[battleController.PredictBattleEntity.Frame + 1] = data;
        System.Console.WriteLine($"SetFrame [{battleController.PredictBattleEntity.Frame + 1}] -> {data}");
    }

}