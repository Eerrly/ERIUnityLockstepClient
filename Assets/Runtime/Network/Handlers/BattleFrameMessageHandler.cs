using Google.Protobuf;

public struct BattleFrameSyncResult
{
    public bool Success;
    public int Frame;
    public int Diff;
    public int ContiguousFrame;
    public byte RawInput0;
    public byte RawInput1;
    public FrameBuffer.Frame InputFrame;
}

/// <summary>
/// 只负责把服务端帧消息写入本地 FrameBuffer，不处理场景、UI 和重连失败界面。
/// </summary>
public class BattleFrameMessageHandler
{
    private readonly FrameBuffer _frameBuffer;

    public BattleFrameMessageHandler(FrameBuffer frameBuffer)
    {
        _frameBuffer = frameBuffer;
    }

    public BattleFrameSyncResult Handle(pb.S2C_FrameMsg message)
    {
        var inputFrame = FrameBuffer.Frame.defFrame;
        inputFrame.frame = (int)message.Frame;
        inputFrame.playerCount = (int)message.PlayerCount;

        var byteArray = message.Datum.ToByteArray();
        for (int i = 0; i < inputFrame.playerCount; i++)
            inputFrame[i] = new FrameBuffer.Input(byte.MaxValue);

        for (int i = 0; i < BattleSetting.MaxPlayerInRoomCount && i < byteArray.Length; i++)
        {
            if ((message.InputCount & (1 << i)) == (1 << i))
                inputFrame[i] = new FrameBuffer.Input(byteArray[i]);
        }

        var diff = 0;
        var success = _frameBuffer.SyncFrame(inputFrame.frame, ref inputFrame, ref diff);
        return new BattleFrameSyncResult
        {
            Success = success,
            Frame = inputFrame.frame,
            Diff = diff,
            ContiguousFrame = _frameBuffer.LastSetFrameIndex,
            RawInput0 = byteArray.Length > 0 ? byteArray[0] : (byte)0,
            RawInput1 = byteArray.Length > 1 ? byteArray[1] : (byte)0,
            InputFrame = inputFrame
        };
    }
}
