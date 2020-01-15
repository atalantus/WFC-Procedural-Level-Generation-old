using UnityEngine;

namespace WFCLevelGeneration.Util
{
    public static class Debugger
    {
        public static void Log(string msg, LevelGenerator.DebugOutputLevels msgLevel,
            LevelGenerator.DebugOutputLevels setLevel, GameObject caller)
        {
            if (msgLevel > setLevel) return;

            switch (msgLevel)
            {
                case LevelGenerator.DebugOutputLevels.Runtime:
                    Debug.Log($"<color=blue>{msg}</color>", caller);
                    break;
                case LevelGenerator.DebugOutputLevels.All:
                    Debug.Log($"{caller.name} | {msg}", caller);
                    break;
            }
        }
    }
}