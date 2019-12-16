﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelGeneration
{
    /// <summary>
    /// Holds module assets
    /// </summary>
    public class ModuleManager : MonoBehaviour
    {
        /// <summary>
        /// The module manager
        /// </summary>
        public static ModuleManager Instance { get; private set; }

        /// <summary>
        /// The modules
        /// </summary>
        public List<Module> modules;

        /// <summary>
        /// The module connections
        /// </summary>
        public ModuleConnections moduleConnections;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

            if (moduleConnections == null)
                moduleConnections = ModuleConnections.Instance;
        }
    }
}