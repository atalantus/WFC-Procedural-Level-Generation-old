using UnityEngine;

namespace WFCLevelGeneration.Util
{
    public static class Debugger
    {
        public static void Log(string msg, WFCBase.DebugOutputLevels msgLevel,
            WFCBase.DebugOutputLevels setLevel, GameObject caller)
        {
            if (msgLevel > setLevel) return;

            switch (msgLevel)
            {
                case WFCBase.DebugOutputLevels.Runtime:
                    Debug.Log($"<color=blue>{msg}</color>", caller);
                    break;
                case WFCBase.DebugOutputLevels.All:
                    Debug.Log($"{caller.name} | {msg}", caller);
                    break;
            }
        }
    }
}