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
    }
}