using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelGeneration
{
    public class ModulesInfo : ScriptableObject
    {
        private static ModulesInfo _instance;

        public static ModulesInfo Instance
        {
            get
            {
                if (!_instance)
                    _instance = Resources.FindObjectsOfTypeAll<ModulesInfo>().FirstOrDefault();
                return _instance;
            }
        }
        
        public Dictionary<string, int> faceConnectionsMap = new Dictionary<string, int>();
    }
}