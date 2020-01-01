using System;
using System.Collections.Generic;
using System.Linq;
using LevelGeneration.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelGeneration
{
    public class ModulesInfo : ScriptableObject
    {
        private int _index = 1; // value 0 is reserved for "adjacent to nothing"
        public ConnectionsDictionary generatedConnections = new ConnectionsDictionary();

        public void AddFace(bool isManual, int hash)
        {
            if (!isManual || hash == 0)
            {
                if (generatedConnections.ContainsKey(hash))
                    generatedConnections[hash]++;
                else
                    generatedConnections.Add(hash, 1);
            }
            else
            {
                while (generatedConnections.ContainsKey(_index))
                {
                    _index++;
                    if (_index == 0)
                        throw new Exception("No more available Hashes in int range! " +
                                            "This is most likely a bug.");
                }

                generatedConnections.Add(_index, 1);
            }

            EditorUtility.SetDirty(this);
        }
    }
}