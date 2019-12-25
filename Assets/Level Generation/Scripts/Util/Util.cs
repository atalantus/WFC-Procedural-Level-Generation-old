using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LevelGeneration
{
    public static class Util
    {
        public static void PopulateCollection<T>(this IList<T> coll) where T : class, new()
        {
            for (int i = 0; i < coll.Count; i++)
            {
                coll[i] = new T();
            }
        }

        public static void DebugLog(string msg, LevelGenerator.DebugOutputLevels msgLevel,
            LevelGenerator.DebugOutputLevels setLevel, GameObject caller)
        {
            if (msgLevel > setLevel) return;

            switch (msgLevel)
            {
                case LevelGenerator.DebugOutputLevels.Runtime:
                    Debug.Log($"<color=blue>{msg}</color>", caller);
                    break;
                case LevelGenerator.DebugOutputLevels.All:
                    Debug.Log($"<color=white>{msg}</color>", caller);
                    break;
            }
        }

        public static void test()
        {
            var s = "dffdff {0} dfdf";
        }
    }
}