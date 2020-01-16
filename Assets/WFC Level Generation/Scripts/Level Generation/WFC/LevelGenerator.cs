using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WFCLevelGeneration.Util;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Debugger = WFCLevelGeneration.Util.Debugger;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace WFCLevelGeneration
{
    /// <summary>
    /// Generates level using the wave-function-collapse algorithm.
    /// Singleton class.
    /// </summary>
    [ExecuteInEditMode]
    public abstract class LevelGenerator : GridGenerator
    {
        #region Attributes

        /// <summary>
        /// The Level Generator
        /// </summary>
        public static LevelGenerator Instance { get; private set; }

        /// <summary>
        /// The general modules
        /// </summary>
        [Header("Level Modules")] [Tooltip("The generally given set of Modules for this level generation")]
        public Module[] generalModules;

        /// <summary>
        /// Specifies only a subset of modules for specific cells
        /// </summary>
        [Tooltip("Specify a different set of modules for specific cells")]
        public SpecialCell[] specialCells;

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
        /// When set to true after the algorithm is done the adjacency
        /// for each cell in the grid will be checked again.
        /// </summary>
        [Tooltip(
            "When set to true after the algorithm is done the adjacency " +
            "for each cell in the grid will be checked again.")]
        public bool validateCellAdjacency = false;

        /// <summary>
        /// The time in seconds between placing the final module in the cell.
        /// </summary>
        [Tooltip("The time in seconds between placing the final module in the cell.")]
        public float modulePlacingStepTime = 0f;

        /// <summary>
        /// Sets level of debug output.
        /// </summary>
        [Tooltip(
            "Sets level of debug output:\n" +
            "None = No debug output\n" +
            "Runtime = Outputs algorithm's execution time\n" +
            "All = Complete debug output")]
        public DebugOutputLevels debugOutputLevel = DebugOutputLevels.Runtime;

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
        /// RNG seed
        /// </summary>
        [Tooltip("The generation seed. -1 means a random seed will be chosen.")]
        public int seed = -1;

        /// <summary>
        /// The wfc algorithms cell history
        /// </summary>
        public List<CellHistory> cellHistories;

        private System.Random _rng;

        #endregion

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
#if UNITY_EDITOR
                DestroyImmediate(gameObject);
#else
                Destroy(gameObject);
#endif
            }

            OnAwake();
        }

        /// <summary>
        /// Unity's Awake event function.
        /// </summary>
        protected virtual void OnAwake()
        {
        }

        private bool RemoveModule(List<Cell> previousCells)
        {
            var sortedCells = new List<Cell>(previousCells);

            // sort cells
            sortedCells.Sort((c1, c2) => c1.SolvedScore.CompareTo(c2.SolvedScore));

            // get cell to change in this step
            while (sortedCells.Count > 0)
            {
                var cell = sortedCells[0];

                if (cell.SolvedScore > 1 || !cell.isCellSet)
                {
                    break;
                }

                // this cell is already set
                sortedCells.RemoveAt(0);
            }

            if (sortedCells.Count == 0)
            {
                // every cell is already set
                // we're done
                return true;
            }

            // try changing the chosen cell
            if (sortedCells[0].SolvedScore == 1)
            {
                // try setting this cell
                var success = sortedCells[0].SetLastModule();

                if (!success)
                {
                    cellHistories.Add(new CellHistory(CellHistory.CellActions.Reset, sortedCells[0],
                        sortedCells[0].possibleModules[0]));
                    return false;
                }

                success = RemoveModule(sortedCells);

                if (!success)
                {
                    cellHistories.Add(new CellHistory(CellHistory.CellActions.Reset, sortedCells[0],
                        sortedCells[0].possibleModules[0]));
                }

                return success;
            }

            // save cell states
            var sortedCellsStates = new Cell.CellState[sortedCells.Count];

            for (var i = 0; i < sortedCells.Count; i++)
            {
                sortedCellsStates[i] = new Cell.CellState(sortedCells[i]);
            }

            // try every cell sorted after their entropy until success
            foreach (var cell in sortedCells)
            {
                // remove modules from this cell until success
                var modules = new List<Module>(cell.possibleModules).ToArray();
                _rng.Shuffle(modules);

                foreach (var module in modules)
                {
                    if (cell.RemoveModule(module))
                    {
                        var success = RemoveModule(sortedCells);

                        if (success) return true;
                    }

                    // removing this module didn't work
                    // reset cell states and try next module
                    for (var i = 0; i < sortedCells.Count; i++)
                    {
                        sortedCells[i].ResetCellState(sortedCellsStates[i]);
                    }
                }

                // no module of this cell works
                // try next cell in entropy sorted list
            }

            // nothing worked :(
            return false;
        }

        /// <summary>
        /// Executes the Wave-function-collapse algorithm
        /// </summary>
        private void WaveFunctionCollapse()
        {
            var wfcSeed = seed != -1 ? seed : Environment.TickCount;

            // Set RNG seed
            _rng = new System.Random(wfcSeed);

            cellHistories = new List<CellHistory>();

            // Instantiate initial SortedCellsList
            var initialCells = new List<Cell>();

            for (var i = 0; i < cells.GetLength(0); i++)
            for (var j = 0; j < cells.GetLength(1); j++)
            for (var k = 0; k < cells.GetLength(2); k++)
            {
                // Populate cell's possibility space
                var specialCell = specialCells.FirstOrDefault(x => x.cellPos == new Vector3(i, j, k));
                cells[i, j, k].PopulateCell(specialCell != null ? specialCell.cellModules : generalModules);

                // Add cell to initial SortedCellsList
                initialCells.Add(cells[i, j, k]);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Debugger.Log($"Starting Wave-function-collapse algorithm with Seed {wfcSeed}", DebugOutputLevels.All,
                debugOutputLevel,
                gameObject);

            var applyInitConstr = new Stopwatch();
            applyInitConstr.Start();

            Debugger.Log("Applying initial constraints", DebugOutputLevels.All, debugOutputLevel, gameObject);

            // Make sure the level fits our initial constraints
            ApplyInitialConstraints();

            applyInitConstr.Stop();

            // Start recursive Wave-Function-Collapse Algorithm
            var success = RemoveModule(initialCells);

            if (!success)
            {
                Debug.LogWarning("WTF ich hab keine ahnung was mein leben is");
            }

            var finishLevelStpwtch = new Stopwatch();
            finishLevelStpwtch.Start();

            Debugger.Log("Applying final constraints", DebugOutputLevels.All, debugOutputLevel, gameObject);

            // Add end constraints
            ApplyFinalConstraints();

            finishLevelStpwtch.Stop();

            stopwatch.Stop();

            Debugger.Log($"Applying initial constraints took {applyInitConstr.Elapsed.TotalMilliseconds}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            Debugger.Log(
                $"Applying final constraints took {finishLevelStpwtch.Elapsed.TotalMilliseconds}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            Debugger.Log(
                $"Complete Wave-function-collapse algorithm finished in {stopwatch.Elapsed.TotalMilliseconds}ms (Seed: {wfcSeed})",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);

            if (modulePlacingStepTime > 0f && Application.isPlaying) StartCoroutine(PlaceFinalModulesDebug());
            else PlaceFinalModules();

            if (validateCellAdjacency) CheckGeneratedLevel();
        }

        private void PlaceFinalModules()
        {
            for (var i = 0; i < cellHistories.Count; i++)
            {
                // TODO: Performance
                cellHistories[i].Execute();
            }
        }

        private IEnumerator PlaceFinalModulesDebug()
        {
            foreach (var cellHistory in cellHistories)
            {
                cellHistory.Execute();

                yield return new WaitForSeconds(modulePlacingStepTime);
            }
        }

        /// <summary>
        /// Checks if the cells of the generated level matches with each other.
        /// </summary>
        /// <returns>True if all of adjacent modules are valid</returns>
        public bool CheckGeneratedLevel()
        {
            const string debugStr = "<color=red>CheckGeneratedLevel | Cell ({0}, {1}, {2}) not adjacent to";
            var isValid = true;

            for (var x = 0; x < cells.GetLength(0); x++)
            for (var y = 0; y < cells.GetLength(1); y++)
            for (var z = 0; z < cells.GetLength(2); z++)
            {
                var cell = cells[x, y, z];
                var fCell = cell.neighbourCells[0];
                var uCell = cell.neighbourCells[1];
                var rCell = cell.neighbourCells[2];

                if (fCell != null)
                    if (cell.possibleModules[0].faceConnections[0] !=
                        fCell.possibleModules[0].faceConnections[3])
                    {
                        isValid = false;
                        Debug.LogError(string.Format(debugStr + " ({0}, {1}, {3})</color>", x, y, z, z + 1));
                    }


                if (uCell != null)
                    if (cell.possibleModules[0].faceConnections[1] !=
                        uCell.possibleModules[0].faceConnections[4])
                    {
                        isValid = false;
                        Debug.LogError(string.Format(debugStr + " ({0}, {3}, {2})</color>", x, y, z, y + 1));
                    }


                if (rCell != null)
                    if (cell.possibleModules[0].faceConnections[2] !=
                        rCell.possibleModules[0].faceConnections[5])
                    {
                        isValid = false;
                        Debug.LogError(string.Format(debugStr + " ({3}, {1}, {2})</color>", x, y, z, x + 1));
                    }
            }

            return isValid;
        }

        /// <summary>
        /// Starts Wave-function-collapse algorithm
        /// </summary>
        public void GenerateLevel()
        {
            RemoveGrid();

            GenerateGrid();

            // Wave-function-collapse algorithm
            WaveFunctionCollapse();
        }

        /// <summary>
        /// Resolve all initial constraints
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        protected virtual void ApplyInitialConstraints()
        {
        }

        /// <summary>
        /// Resolve all final constraints
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        protected virtual void ApplyFinalConstraints()
        {
        }
    }

    [Serializable]
    public class SpecialCell
    {
        /// <summary>
        /// The cell position
        /// </summary>
        public Vector3 cellPos;

        /// <summary>
        /// The subset of possible modules for this cell
        /// </summary>
        public Module[] cellModules;
    }

    [Serializable]
    public class CellHistory
    {
        public enum CellActions
        {
            Set,
            Reset
        }

        public CellActions action;
        public Module module;
        public Cell cell;

        public CellHistory(CellActions action, Cell cell, Module module = null)
        {
            this.action = action;
            this.cell = cell;
            this.module = module;
        }

        public void Execute()
        {
            switch (action)
            {
                case CellActions.Set:
                    // Instantiate module game object
                    var go = Object.Instantiate(module.moduleGO, cell.transform.position,
                        module.moduleGO.transform.rotation);
                    go.transform.parent = cell.transform;
                    cell.placedModule = go;
                    break;
                case CellActions.Reset:
                    if (Application.isPlaying) Object.Destroy(cell.placedModule);
                    else Object.DestroyImmediate(cell.placedModule);
                    break;
            }
        }
    }
}