using System.Collections.Generic;

public class ReplaySystem
{
    private static FrameBuffer.Input[] _lastPlayerInputs;
    private static Dictionary<int, List<FrameBuffer.Input>> _frameBufferLog;

    public static Dictionary<int, List<FrameBuffer.Input>> FrameBufferLog => _frameBufferLog;

    public static void Init()
    {
        _lastPlayerInputs = new FrameBuffer.Input[GameSetting.RoomMaxPlayerCount];
        _frameBufferLog = new Dictionary<int, List<FrameBuffer.Input>>(BattleSetting.MaxFrameCount);
    }

    public static void Release()
    {
        if (_frameBufferLog != null)
        {
            _frameBufferLog.Clear();
            _frameBufferLog = null;
        }
    }

    public static void SaveFrame(FrameBuffer.Frame playerInput)
    {
        if (!_frameBufferLog.TryGetValue(playerInput.frame, out var frameInputList))
        {
            frameInputList = new List<FrameBuffer.Input>(playerInput.playerCount);
            _frameBufferLog[playerInput.frame] = frameInputList;
        }
        for (var i = 0; i < playerInput.playerCount; i++)
        {
            var input = new FrameBuffer.Input();
            if (playerInput.GetInputByPos(i, ref input))
            {
                if (!_lastPlayerInputs[i].Compare(input))
                {
                    _lastPlayerInputs[i] = input;
                    frameInputList.Add(input);
                }
                else
                    continue;
            }
        }
    }

}