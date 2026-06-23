/// <summary>
/// 帧数据管理器
/// </summary>
public class FrameBuffer
{

    /// <summary>
    /// 操作数据
    /// </summary>
    public struct Input
    {
        /// <summary>
        /// 8 - bit
        /// | 0 0 0 0 | 0 0 0 | 0 |
        /// |   yaw   |  key  |pos|
        /// yaw  :   4 bit   :   (read & 0xF0) >> 4
        /// key  :   3 bit   :   (read & 0x0E) >> 1
        /// pos  :   1 bit   :   (read & 0x01)
        /// </summary>
        private byte raw;

        /// <summary>
        /// 8  1  2
        ///  \ | /
        /// 7——0——3
        ///  / | \
        /// 6  5  4
        /// </summary>
        public byte yaw
        {
            get => (byte)((0xF0 & raw) >> 4);
            set => raw = (byte)((raw & ~0xF0) | ((0xF & value) << 4));
        }

        /// <summary>
        /// [j] [k] [l]
        /// </summary>
        public byte key
        {
            get => (byte)((0x0E & raw) >> 1);
            set => raw = (byte)((raw & ~0x0E) | ((0x7 & value) << 1));
        }

        /// <summary>
        /// 玩家POS
        /// </summary>
        public byte pos
        {
            get => (byte)(0x01 & raw);
            set => raw = (byte)((raw & ~0x01) | value);
        }

        public Input(byte value)
        {
            raw = value;
        }

        public Input(byte pos, byte value)
        {
            raw = (byte)(~0x01 & (value << 1) | pos);
        }

        public override string ToString()
        {
            return $"pos:{pos}, raw:{raw}, yaw:{yaw}, btn:{key}";
        }

        /// <summary>
        /// 原始数据
        /// </summary>
        /// <returns>原始数据</returns>
        public byte ToByte()
        {
            return raw;
        }

        /// <summary>
        /// 比较操作数据是否相同
        /// </summary>
        /// <param name="other">操作数据</param>
        /// <returns>是否相同</returns>
        public bool Compare(Input other)
        {
            return yaw == other.yaw && key == other.key;
        }

    }

    /// <summary>
    /// 帧数据
    /// </summary>
    public struct Frame
    {
        /// <summary>
        /// 帧号
        /// </summary>
        public int frame;
        /// <summary>
        /// 玩家数量
        /// </summary>
        public int playerCount;
        /// <summary>
        /// 玩家战斗POS:0的操作数据
        /// </summary>
        public Input i0;
        /// <summary>
        /// 玩家战斗POS:1的操作数据
        /// </summary>
        public Input i1;

        /// <summary>
        /// 默认操作数据
        /// </summary>
        public static readonly Input defInput = new Input();
        /// <summary>
        /// 默认帧数据
        /// </summary>
        public static readonly Frame defFrame = new Frame()
        {
            frame = 0,
            playerCount = 0,
            i0 = new Input(0, 0),
            i1 = new Input(1, 0),
        };

        /// <summary>
        /// 玩家数量
        /// </summary>
        public int Length => playerCount;

        public Input this[int index]
        {
            get
            {
                if (index < playerCount)
                {
                    switch (index)
                    {
                        case 0: return i0;
                        case 1: return i1;
                    }
                }
                return defInput;
            }
            set
            {
                if (index < playerCount)
                {
                    switch (index)
                    {
                        case 0: i0 = value; break;
                        case 1: i1 = value; break;
                    }
                }
            }
        }

        /// <summary>
        /// 通过玩家战斗POS设置对应的操作数据
        /// </summary>
        /// <param name="pos">玩家POS</param>
        /// <param name="result">操作数据</param>
        public void SetInputByPos(int pos, Input result)
        {
            if (i0.pos == pos)
            {
                i0 = result;
                return;
            }
            if (i1.pos == pos)
            {
                i1 = result;
                return;
            }
            Logger.Log(LogLevel.Warning,$"FrameBuffer.SetInputByPos pos not found! {pos},{playerCount},{frame}");
        }

        /// <summary>
        /// 通过玩家战斗POS获取对应的操作数据
        /// </summary>
        /// <param name="pos">战斗POS</param>
        /// <param name="result">操作数据</param>
        /// <returns>是否成功获取</returns>
        public bool GetInputByPos(int pos, ref Input result)
        {
            if (i0.pos == pos)
            {
                result = i0;
                return true;
            }
            if (i1.pos == pos)
            {
                result = i1;
                return true;
            }
            Logger.Log(LogLevel.Warning,$"FrameBuffer.GetInputByPos pos not found! {pos},{playerCount},{frame}");
            return false;
        }
        
        public override string ToString()
        {
            return $"[frame:{frame}, playerCount:{playerCount}, (i0:{i0}, i1:{i1})]";
        }
    }


    private int playerCount;
    private int capacity;
    /// <summary>
    /// 操作数据的字节长度
    /// </summary>
    private int inputSize;
    /// <summary>
    /// 帧数据的字节长度
    /// </summary>
    private int frameSize;
    /// <summary>
    /// 帧数据缓存数组
    /// </summary>
    private byte[] buffer;

    private Frame _lastGetFrame = Frame.defFrame;
    /// <summary>
    /// 最近一次同步的帧号
    /// </summary>
    private int _lastSetFrameIndex = -1;
    /// <summary>
    /// 最近一次同步的帧号
    /// </summary>
    public int LastSetFrameIndex => _lastSetFrameIndex;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="playerCount">玩家数量</param>
    /// <param name="capacity">缓存长度</param>
    public FrameBuffer(int playerCount, int capacity = 1000)
    {
        var size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Input));
        Logger.Log(LogLevel.Info,$"Create FrameBuffer playerCount:{playerCount} capacity:{capacity} Input.Size:{size}");
        this.playerCount = playerCount;
        this.capacity = capacity;
        this.inputSize = size;

        this.frameSize = inputSize * this.playerCount + 4/*(frame)*/;
        this.buffer = new byte[frameSize * this.capacity];
        for (int i = 0; i < capacity; i++)
        {
            unsafe
            {
                fixed(byte* dest = &buffer[i * frameSize])
                {
                    // frame
                    *(int*)dest = -1;
                }
            }
        }
    }

    /// <summary>
    /// 重置
    /// </summary>
    public void Reset()
    {
        ResetForReconnect(0);
    }

    /// <summary>
    /// 重置重连同步状态
    /// </summary>
    /// <param name="lastSyncedFrame">最后已同步帧</param>
    public void ResetForReconnect(int lastSyncedFrame)
    {
        ResetReadCursor(lastSyncedFrame);
        _lastSetFrameIndex = lastSyncedFrame;
    }

    /// <summary>
    /// 仅重置读取游标
    /// </summary>
    /// <param name="lastReadFrame">最后已读取帧</param>
    public void ResetReadCursor(int lastReadFrame)
    {
        _lastGetFrame = Frame.defFrame;
        _lastGetFrame.frame = lastReadFrame;
    }

    /// <summary>
    /// 是否有某帧的帧数据
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <returns>是否有某帧的帧数据</returns>
    public bool HasFrame(int frame)
    {
        unsafe
        {
            fixed (byte* dest = &buffer[(frame % capacity) * frameSize])
            {
                var currentFrame = *(int*)dest;
                if(frame != currentFrame)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 尝试获取帧数据
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="result">帧数据</param>
    /// <param name="remove">是否获取完一个标记一个，避免重复获取</param>
    /// <returns>是否成功获取到帧数据</returns>
    public bool TryGetFrame(int frame, ref Frame result, bool remove = true)
    {
        unsafe
        {
            result.playerCount = playerCount;
            fixed(byte* dest = &buffer[(frame % capacity) * frameSize])
            {
                if(frame != 0 && _lastGetFrame.frame + 1 != frame)
                {
                    Logger.Log(LogLevel.Error,$"TryGetFrame must frame by frame lastFrame:{_lastGetFrame.frame} currFrame:{frame}");
                    return false;
                }
                var currentFrame = *(int*)dest;
                if(frame != currentFrame)
                {
                    return false;
                }
                result.frame = currentFrame;
                result.playerCount = playerCount;
                
                result.i0.pos = 255;
                result.i1.pos = 255;
                if(playerCount > 0)
                {
                    result.i0 = *(Input*)(dest + 4/*(frame)*/ + 0 * inputSize);
                }
                if(playerCount > 1)
                {
                    result.i1 = *(Input*)(dest + 4/*(frame)*/ + 1 * inputSize);
                }
                if (remove)
                {
                    // 通过帧号的不匹配检查（if (frame != currentFrame)），在帧号被标记为 -1 后，函数会自动返回 false，从而实现跳过处理这个已经被标记为无效的帧数据的效果。
                    *(int*)dest = -1;
                }

                if (frame > 0)
                {
                    for (int i = 0; i < result.playerCount; i++)
                    {
                        // 特殊标记，当帧数据=当前类型的最大值时，说明那个玩家那帧并没有操作，然后得到并保存上一次成功获取的帧数据
                        if (result[i].ToByte() == byte.MaxValue)
                        {
                            result[i] = _lastGetFrame[i];
                            *(Input*)(dest + 4 /*frame*/ + i * inputSize) = result[i];
                        }
                    }
                }
                _lastGetFrame = result;
            }
        }
        Logger.Log(LogLevel.Info,$"TryGetFrame _lastGetFrame:{_lastGetFrame.frame}");
        return true;
    }

    /// <summary>
    /// 同步帧
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="input">输入</param>
    /// <param name="diff">差值</param>
    /// <returns>是否成功同步</returns>
    public bool SyncFrame(int frame, ref Frame input, ref int diff)
    {
        unsafe
        {
            if(frame <= _lastSetFrameIndex)
                return true;

            fixed(byte* dest = &buffer[(frame % capacity) * frameSize])
            {
                diff = frame - _lastSetFrameIndex;
                // 必须要逐帧同步，否则算失败
                if(diff > 1)
                {
                    Logger.Log(LogLevel.Error,$"SyncFrame must frame by frame lastFrame:{_lastSetFrameIndex} currFrame:{frame}");
                    return false;
                }
                if(playerCount > 0)
                {
                    *(Input*)(dest + 4/*(frame)*/ + 0 * inputSize) = input.i0;
                }
                if(playerCount > 1)
                {
                    *(Input*)(dest + 4/*(frame)*/ + 1 * inputSize) = input.i1;
                }
                *(int*)dest = frame;
                _lastSetFrameIndex = frame;
            }
        }
        return true;
    }

}
