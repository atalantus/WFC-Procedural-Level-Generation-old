using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelGeneration
{
    public class ModulesInfo : ScriptableObject
    {
        private int _counter = 0;
        public ConnectionsDictionary generatedConnections = new ConnectionsDictionary();

        public void AddFace(bool isManual, int hash = 0)
        {
            if (!isManual)
            {
                if (generatedConnections.ContainsKey(hash))
                    generatedConnections[hash]++;
                else
                    generatedConnections.Add(hash, 1);
            }
            else
            {
                while (generatedConnections.ContainsKey(_counter))
                {
                    _counter++;
                    if (_counter == -1)
                        throw new Exception("No more available Hashes in int range! " +
                                            "This is most likely a bug.");
                }

                generatedConnections.Add(_counter, 1);
            }
            
            EditorUtility.SetDirty(this);
        }
    }
}