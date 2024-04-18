// A simple logger class that uses Console.WriteLine by default.
// Can also do Logger.LogMethod = Debug.Log for Unity etc.
// (this way we don't have to depend on UnityEngine)
using System;

namespace kcp2k
{
    public static class Log
    {
        public static Action<string> Info    = (msg) => { Logger.Log(LogLevel.Info, msg); };
        public static Action<string> Warning = (msg) => { Logger.Log(LogLevel.Warning, msg); };
        public static Action<string> Error   = (msg) => { Logger.Log(LogLevel.Error, msg); };
    }
}
