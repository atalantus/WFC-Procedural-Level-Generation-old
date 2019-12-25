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
                // value 0 and 1 are reserved for "adjacent to nothing" and "adjacent to everything"

                var i = 2;
                while (generatedConnections.ContainsKey(i))
                {
                    i++;
                    if (i == 0)
                        throw new Exception("No more available Hashes in int range! " +
                                            "This is most likely a bug.");
                }

                generatedConnections.Add(i, 1);
            }

            EditorUtility.SetDirty(this);
        }
    }
}