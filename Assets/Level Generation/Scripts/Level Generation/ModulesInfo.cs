using System;
using LevelGeneration.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace LevelGeneration
{
    [Serializable]
    public class ModulesInfo : ScriptableObject
    {
        private int _index = 1; // value 0 is reserved for "adjacent to nothing"
        public ConnectionsDictionary generatedConnections = new ConnectionsDictionary();

        public int GenerateNewFaceId()
        {
            while (generatedConnections.ContainsKey(_index))
            {
                _index++;
                if (_index == 0)
                    throw new Exception("No more available Hashes in int range! " +
                                        "This is most likely a bug.");
            }

            return _index;
        }

        public void AddFace(int hash)
        {
            if (generatedConnections.ContainsKey(hash))
                ++generatedConnections[hash];
            else
                generatedConnections.Add(hash, 1);

            EditorUtility.SetDirty(this);
        }

        public void RemoveFace(int hash)
        {
            if (generatedConnections.ContainsKey(hash))
                --generatedConnections[hash];

            if (generatedConnections[hash] == 0)
                generatedConnections.Remove(hash);

            EditorUtility.SetDirty(this);
        }
    }
}