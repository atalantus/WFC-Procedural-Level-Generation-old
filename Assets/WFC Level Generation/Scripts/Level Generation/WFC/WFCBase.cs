using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using WFCLevelGeneration.Constraints;
using Debug = UnityEngine.Debug;
using Debugger = WFCLevelGeneration.Util.Debugger;

namespace WFCLevelGeneration
{
    /// <summary>
    /// The base class for Wave-function-collapse Algorithms.
    /// </summary>
    public abstract class WFCBase : GridGenerator
    {
        #region Attributes

        /// <summary>
        /// The general modules
        /// </summary>
        [Header("Level Modules")] [Tooltip("The generally given set of Modules for this level generation")]
        public Module[] generalModules;

        /// <summary>
        /// Specifies only a subset of modules for specific cells
        /// </summary>
        [Tooltip("Specify a different set of modules for specific cells")]
        public ManualCell[] manualCells;

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
        /// RNG seed.
        /// </summary>
        [Tooltip("The generation seed. -1 means a random seed will be chosen.")]
        public int seed = -1;

        /// <summary>
        /// The wfc algorithms cell history.
        /// </summary>
        [HideInInspector] public List<Cell.CellHistory> cellHistories;

        protected System.Random rng;
        private IGenerationConstraint[] _initialConstraints;
        private IGenerationConstraint[] _finalConstraints;

        #endregion

        #region Methods

        /// <summary>
        /// The main part of the Wave-function-collapse algorithm.
        /// Tries collapsing the Wave-function.
        /// </summary>
        /// <returns>Was the Wave-function successfully collapsed</returns>
        protected abstract bool CollapseWaveFunction();

        /// <summary>
        /// Resolves all initial constraints.
        /// </summary>
        /// <returns>Execution time in milliseconds</returns>
        private long ApplyInitialConstraints()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Debugger.Log("Applying initial constraints", DebugOutputLevels.All, debugOutputLevel, gameObject);

            if (_initialConstraints != null)
                foreach (var constraint in _initialConstraints)
                {
                    constraint.Execute(cells);
                }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Resolves all final constraints.
        /// </summary>
        /// <returns>Execution time in milliseconds</returns>
        private long ApplyFinalConstraints()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Debugger.Log("Applying final constraints", DebugOutputLevels.All, debugOutputLevel, gameObject);

            if (_finalConstraints != null)
                foreach (var constraint in _finalConstraints)
                {
                    constraint.Execute(cells);
                }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Populates the cells
        /// </summary>
        private void PopulateCells()
        {
            for (var i = 0; i < cells.GetLength(0); i++)
            for (var j = 0; j < cells.GetLength(1); j++)
            for (var k = 0; k < cells.GetLength(2); k++)
            {
                // Populate cell's possibility space
                var manualCell = manualCells?.FirstOrDefault(x => x.cellPos == new Vector3(i, j, k));
                cells[i, j, k].PopulateCell(manualCell != null ? manualCell.cellModules : generalModules);
            }
        }

        private IEnumerator InstantiateModuleObjectsDebug()
        {
            foreach (var cellHistory in cellHistories)
            {
                cellHistory.Execute();

                yield return new WaitForSeconds(modulePlacingStepTime);
            }
        }

        /// <summary>
        /// Run the Wave-function-collapse algorithm
        /// </summary>
        /// <returns>Was the Wave-function successfully collapsed</returns>
        public bool RunWFC()
        {
            var wfcSeed = seed != -1 ? seed : Environment.TickCount;

            Debugger.Log($"Starting Wave-function-collapse algorithm with Seed {wfcSeed}", DebugOutputLevels.All,
                debugOutputLevel,
                gameObject);

            rng = new System.Random(wfcSeed);
            cellHistories = new List<Cell.CellHistory>();

            PopulateCells();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var initCnstrTime = ApplyInitialConstraints();

            var success = CollapseWaveFunction();

            var finalCnstrTime = ApplyFinalConstraints();

            stopwatch.Stop();

            Debugger.Log($"Applying initial constraints took {initCnstrTime}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            Debugger.Log(
                $"Applying final constraints took {finalCnstrTime}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            Debugger.Log(
                $"Complete Wave-function-collapse algorithm finished in {stopwatch.Elapsed.TotalMilliseconds}ms (Seed: {wfcSeed})",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);

            return success;
        }

        /// <summary>
        /// Instantiates the actual Module GameObjects of collapsed cells.
        /// </summary>
        public void InstantiateModuleObjects()
        {
            if (modulePlacingStepTime > 0f && Application.isPlaying && cellHistories != null)
            {
                StartCoroutine(InstantiateModuleObjectsDebug());
            }
            else
            {
                for (var x = 0; x < cells.GetLength(0); x++)
                for (var y = 0; y < cells.GetLength(1); y++)
                for (var z = 0; z < cells.GetLength(2); z++)
                {
                    var cell = cells[x, y, z];
                    if (!cell.isCellSet) continue;

                    var module = cell.possibleModules[0];

                    var go = Instantiate(module.moduleGO, cell.transform.position,
                        module.moduleGO.transform.rotation);
                    go.transform.parent = cell.transform;
                    cell.placedModule = go;
                }
            }
        }

        /// <summary>
        /// Checks if the cells of the generated level matches with each other.
        /// </summary>
        /// <returns>True if all of adjacent modules are valid</returns>
        public bool CheckAdjacency()
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
        /// Generate the Level.
        /// </summary>
        public void GenerateLevel()
        {
            RemoveGrid();
            GenerateGrid(this);

            RunWFC();
            InstantiateModuleObjects();
            // TODO: Only for development purposes. Remove before making asset public.
            CheckAdjacency();
        }

        /// <summary>
        /// Set the initial constraints.
        /// </summary>
        /// <param name="constraints">The constraints</param>
        public void SetInitialConstraints(params IGenerationConstraint[] constraints)
        {
            _initialConstraints = constraints;
        }

        /// <summary>
        /// Set the final constraints.
        /// </summary>
        /// <param name="constraints">The constraints</param>
        public void SetFinalConstraints(params IGenerationConstraint[] constraints)
        {
            _finalConstraints = constraints;
        }

        #endregion

        [Serializable]
        public class ManualCell
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
    }
}