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

        /// <summary>
        /// Stores the cells in a heap having the closest cell to being solved as first element
        /// </summary>
        public Heap<Cell> orderedCells;

        // TODO
        private Cell backtrackLastCell;
        private int backtrackLastCellScore;

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

        /// <summary>
        /// Executes the Wave-function-collapse algorithm
        /// </summary>
        private void WaveFunctionCollapse()
        {
            var wfcSeed = seed != -1 ? seed : Environment.TickCount;

            // Set RNG seed
            Random.InitState(wfcSeed);

            cellHistories = new List<CellHistory>();

            // Instantiate cells heap
            orderedCells = new Heap<Cell>(cells.GetLength(0) * cells.GetLength(1) * cells.GetLength(2));

            for (var i = 0; i < cells.GetLength(0); i++)
            for (var j = 0; j < cells.GetLength(1); j++)
            for (var k = 0; k < cells.GetLength(2); k++)
            {
                // Populate cell's possibility space
                var specialCell = specialCells.FirstOrDefault(x => x.cellPos == new Vector3(i, j, k));
                cells[i, j, k].PopulateCell(specialCell != null ? specialCell.cellModules : generalModules);

                // Add cell to heap
                orderedCells.Add(cells[i, j, k]);
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

            // Wave-function-collapse Algorithm
            while (true)
            {
                Debugger.Log("Starting another iteration! Removing next module.", DebugOutputLevels.All,
                    debugOutputLevel, gameObject);

                // Remove finished cells from heap
                while (orderedCells.Count > 0)
                {
                    var cell = orderedCells.GetFirst();

                    if (cell.SolvedScore <= 0)
                        Debug.LogError(
                            $"Impossible Map! No fitting module could be found for {cell}. solved Score: {cell.SolvedScore}",
                            gameObject);

                    if (cell.SolvedScore == 1)
                    {
                        if (cell.isCellSet)
                        {
                            orderedCells.RemoveFirst();
                        }
                        else
                        {
                            if (!cell.SetModule(cell.possibleModules[0]))
                                Debug.LogError($"Backtrack Error while setting module {cell}!");

                            goto iteration_end;
                        }
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

                    if (backtrackLastCell == cell && backtrackLastCellScore == cell.SolvedScore)
                    {
                        Debug.LogError($"Infinite Loop prevention on {cell.name} with {cell.SolvedScore}!");
                        return;
                    }

                    backtrackLastCell = cell;
                    backtrackLastCellScore = cell.SolvedScore;

                    var c = 0;
                    var mLength = cell.possibleModules.Count;
                    var i = Random.Range(0, cell.possibleModules.Count);

                    while (c < mLength)
                    {
                        i = ++i % mLength;

                        if (cell.RemoveModule(cell.possibleModules[i])) break;

                        ++c;

                        if (c == mLength)
                            Debug.LogWarning($"Couldn't backtrack far enough {cell.name} with {cell.SolvedScore}!");
                    }
                }
                else
                {
                    // Finished
                    break;
                }

                iteration_end: ;
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
                if (cellHistories[i].action == CellHistory.CellActions.Reset) continue;
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
        public Cell cell;

        public CellHistory(CellActions action, Cell cell)
        {
            this.action = action;
            this.cell = cell;
        }

        public void Execute()
        {
            switch (action)
            {
                case CellActions.Set:
                    // Instantiate module game object
                    var moduleGo = cell.possibleModules[0].moduleGO;
                    var go = Object.Instantiate(moduleGo, cell.transform.position, moduleGo.transform.rotation);
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