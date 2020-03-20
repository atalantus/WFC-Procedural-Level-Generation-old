using System;
using UnityEngine;

namespace WFCLevelGeneration.Editor
{
    public static class ModuleGeneration
    {
        public delegate void NextModuleData(ModuleData data);

        public static event NextModuleData OnNextModuleData;

        public static void GenerateModuleCell()
        {
            // TODO
        }

        public static void GenerateNextModule()
        {
            // TODO
            OnNextModuleData?.Invoke(new ModuleData());
        }

        public static Vector3 GetNextLayoutPos(Vector2 stepSize, int n)
        {
            var pos = new Vector3();

            var rc = Mathf.CeilToInt((float) Math.Sqrt(n));
            var rcc = Mathf.Pow(rc, 2);
            var abs = Mathf.Abs(rcc - n);
            var z = Mathf.CeilToInt(abs / 2f);

            var x = rc - z * (abs % 2);
            var y = rc - z * (1 - abs % 2);

            pos.x = x * (stepSize.x * 1.5f);
            pos.z = -y * (stepSize.y * 1.5f);

            return pos;
        }

        public class ModuleData
        {
            // TODO
        }
    }
}