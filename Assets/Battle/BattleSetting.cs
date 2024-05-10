using UnityEngine;

public class BattleSetting
{
    public const int NetInterval = 1;
    
    public const int BattleInterval = 33;
    
    public const int HeartbeatTime = 1000;
    
    public const int MaxPlayerInRoomCount = 2;
    
    public const int MaxFrameCount = 10000;
    
    public const int MaxPredictFrameCount = 4;
    
    public const string PlayerCharacterPath = "Cube";
    
    public static readonly Color[] InitPlayerColor = { new Color((float)42/255, (float)100 /255, (float)178 /255), new Color((float)229/255, (float)46 /255, (float)40 /255) };
}