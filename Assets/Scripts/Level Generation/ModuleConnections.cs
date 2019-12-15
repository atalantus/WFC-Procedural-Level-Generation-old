using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelGeneration
{
    [CreateAssetMenu(fileName = "Module Connections", menuName = "Map Generation/Module Connections")]
    public class ModuleConnections : ScriptableObject
    {
        private static ModuleConnections _instance;

        public static ModuleConnections Instance
        {
            get
            {
                if (!_instance)
                    _instance = Resources.FindObjectsOfTypeAll<ModuleConnections>().FirstOrDefault();
                return _instance;
            }
        }

        public Dictionary<string, int> faceConnectionsMap = new Dictionary<string, int>();
    }
}