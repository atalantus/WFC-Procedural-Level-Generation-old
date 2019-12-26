﻿using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace LevelGeneration
{
    /// <summary>
    /// Generates level using the wave-function-collapse algorithm.
    /// Singleton class.
    /// </summary>
    public abstract class LevelGenerator : MonoBehaviour
    {
        #region Attributes

        /// <summary>
        /// The Level Generator
        /// </summary>
        public static LevelGenerator Instance { get; private set; }

        /// <summary>
        /// The modules
        /// </summary>
        [Header("Level Modules")] [Tooltip("The given set of Modules for this level generation")]
        public List<Module> modules;

        /// <summary>
        /// When set to true the algorithm will run on a separate
        /// thread instead of blocking Unity's main thread. Recommend setting this option to true!
        /// </summary>
        [Header("Options")]
        [Tooltip(
            "When set to true (recommended) the algorithm will run on a separate " +
            "thread instead of blocking Unity's main thread."
        )]
        public bool runInExtraThread = true;

        /// <summary>
        /// Sets level of debug output.
        /// </summary>
        [Tooltip(
            "Sets level of debug output:\n" +
            "None = No debug output\n" +
            "Runtime = Outputs algorithm's execution time\n" +
            "All = Complete debug output")]
        public DebugOutputLevels debugOutputLevel = DebugOutputLevels.Runtime;

        /// <summary>
        /// When set to true after the algorithm is done the adjacency
        /// for each cell in the grid will be checked again.
        /// </summary>
        [Tooltip(
            "When set to true after the algorithm is done the adjacency " +
            "for each cell in the grid will be checked again.")]
        public bool validateCellAdjacency = false;

        public enum DebugOutputLevels
        {
            /// <summary>
            /// No debug output
            /// </summary>
            None = 0,

            /// <summary>
            /// Outputs algorithm's execution time
            /// </summary>
            Runtime = 1,

            /// <summary>
            /// Complete debug output
            /// </summary>
            All = 2
        }

        /// <summary>
        /// Stores the cells in a heap having the closest cell to being solved as first element
        /// </summary>
        [HideInInspector] public Heap<Cell> orderedCells;

        #endregion

        #region Methods

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

            OnAwake();
        }

        /// <summary>
        /// Unity's Awake event function.
        /// </summary>
        protected virtual void OnAwake()
        {
        }

        /// <summary>
        /// Wave-function-collapse algorithm
        /// TODO: Could be multithreaded to increase performance
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        /// <param name="seed">RNG seed</param>
        public void GenerateLevelWFC(ref Cell[,,] cells, int seed)
        {
            // Set RNG seed
            Random.InitState(seed);

            // Instantiate cells heap
            orderedCells = new Heap<Cell>(cells.GetLength(0) * cells.GetLength(1) * cells.GetLength(2));

            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    for (int k = 0; k < cells.GetLength(2); k++)
                    {
                        orderedCells.Add(cells[i, j, k]);
                    }
                }
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Util.DebugLog("Starting Wave-function-collapse algorithm", DebugOutputLevels.All, debugOutputLevel,
                gameObject);

            var applyInitConstr = new Stopwatch();
            applyInitConstr.Start();

            Util.DebugLog("Applying initial constraints", DebugOutputLevels.All, debugOutputLevel, gameObject);

            // Make sure the level fits our initial constraints
            ApplyInitialConstraints(ref cells);

            applyInitConstr.Stop();

            // Wave-function-collapse Algorithm
            while (true)
            {
                Util.DebugLog("Starting another iteration! Removing next module.", DebugOutputLevels.All,
                    debugOutputLevel, gameObject);

                // Remove finished cells from heap
                while (orderedCells.Count > 0)
                {
                    var cell = orderedCells.GetFirst();

                    if (cell.SolvedScore == 1)
                    {
                        orderedCells.RemoveFirst();
                    }
                    else
                    {
                        break;
                    }
                }

                // Remove random module from cell
                if (orderedCells.Count > 0)
                {
                    var cell = orderedCells.GetFirst();
                    cell.RemoveModule(cell.possibleModulesIndices[Random.Range(0, cell.possibleModulesIndices.Count)]);
                }
                else
                {
                    // Finished
                    break;
                }
            }

            var finishLevelStpwtch = new Stopwatch();
            finishLevelStpwtch.Start();

            Util.DebugLog("Applying FinishLevel", DebugOutputLevels.All, debugOutputLevel, gameObject);

            // Add end constraints
            FinishLevel(ref cells);

            finishLevelStpwtch.Stop();

            stopwatch.Stop();

            Util.DebugLog($"Applying initial constraints took {applyInitConstr.Elapsed.TotalMilliseconds}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            Util.DebugLog(
                $"Applying finishing level took {finishLevelStpwtch.Elapsed.TotalMilliseconds}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            Util.DebugLog(
                $"Complete Wave-function-collapse algorithm finished in {stopwatch.Elapsed.TotalMilliseconds}ms (Seed: {seed})",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);

            if (validateCellAdjacency) CheckGeneratedLevel(ref cells);
        }

        /// <summary>
        /// Checks if the cells of the generated level matches with each other.
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        /// <returns>True if all of adjacent modules are valid</returns>
        public bool CheckGeneratedLevel(ref Cell[,,] cells)
        {
            const string debugStr = "<color=red>CheckGeneratedLevel | Cell ({0}, {1}, {2}) not adjacent to";
            var isValid = true;

            for (int x = 0; x < cells.GetLength(0); x++)
            for (int y = 0; y < cells.GetLength(1); y++)
            for (int z = 0; z < cells.GetLength(2); z++)
            {
                var cell = cells[x, y, z];
                var fCell = cell.neighbourCells[0];
                var uCell = cell.neighbourCells[1];
                var rCell = cell.neighbourCells[2];

                if (fCell != null)
                    if (modules[cell.possibleModulesIndices[0]].faceConnections[0] !=
                        modules[fCell.possibleModulesIndices[0]].faceConnections[3])
                    {
                        isValid = false;
                        Debug.LogError(string.Format(debugStr + " ({0}, {1}, {3})</color>", x, y, z, z + 1));
                    }


                if (uCell != null)
                    if (modules[cell.possibleModulesIndices[0]].faceConnections[1] !=
                        modules[uCell.possibleModulesIndices[0]].faceConnections[4])
                    {
                        isValid = false;
                        Debug.LogError(string.Format(debugStr + " ({0}, {3}, {2})</color>", x, y, z, y + 1));
                    }


                if (rCell != null)
                    if (modules[cell.possibleModulesIndices[0]].faceConnections[2] !=
                        modules[rCell.possibleModulesIndices[0]].faceConnections[5])
                    {
                        isValid = false;
                        Debug.LogError(string.Format(debugStr + " ({3}, {1}, {2})</color>", x, y, z, x + 1));
                    }
            }

            return isValid;
        }

        /// <summary>
        /// Resolve all initial constraints
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        protected abstract void ApplyInitialConstraints(ref Cell[,,] cells);

        /// <summary>
        /// Apply finishing touches to the level
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        protected abstract void FinishLevel(ref Cell[,,] cells);

        #endregion
    }
}