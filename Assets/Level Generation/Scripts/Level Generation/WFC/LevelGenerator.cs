using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using LevelGeneration.Util;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace LevelGeneration.WFC
{
    /// <summary>
    /// Generates level using the wave-function-collapse algorithm.
    /// Singleton class.
    /// </summary>
    [ExecuteInEditMode]
    public abstract class LevelGenerator : MonoBehaviour
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
        /// TODO
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
        /// Grid dimensions
        /// </summary>
        [Header("Grid")] [Tooltip("The dimensions of the grid")]
        public Vector3Int dimensions = new Vector3Int(5, 1, 5);

        /// <summary>
        /// Cell prefab
        /// </summary>
        [Tooltip("The cell prefab")] public GameObject cellPrefab;

        /// <summary>
        /// Cells matrix ([width, height, depth])
        /// </summary>
        [HideInInspector] public Cell[,,] cells;

        /// <summary>
        /// Stores the cells in a heap having the closest cell to being solved as first element
        /// </summary>
        [HideInInspector] public Heap<Cell> orderedCells;

        #endregion

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

        #region WFC-Methods

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
                        // Populate cell's possibility space
                        var specialCell = specialCells.FirstOrDefault(x => x.cellPos == new Vector3(i, j, k));
                        cells[i, j, k].PopulateCell(specialCell != null ? specialCell.cellModules : generalModules);

                        // Add cell to heap
                        orderedCells.Add(cells[i, j, k]);
                    }
                }
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            DebugLogger.Log($"Starting Wave-function-collapse algorithm with Seed {seed}", DebugOutputLevels.All,
                debugOutputLevel,
                gameObject);

            var applyInitConstr = new Stopwatch();
            applyInitConstr.Start();

            DebugLogger.Log("Applying initial constraints", DebugOutputLevels.All, debugOutputLevel, gameObject);

            // Make sure the level fits our initial constraints
            ApplyInitialConstraints(ref cells);

            applyInitConstr.Stop();

            // Wave-function-collapse Algorithm
            while (true)
            {
                DebugLogger.Log("Starting another iteration! Removing next module.", DebugOutputLevels.All,
                    debugOutputLevel, gameObject);

                // Remove finished cells from heap
                while (orderedCells.Count > 0)
                {
                    var cell = orderedCells.GetFirst();

                    if (cell.SolvedScore <= 0)
                    {
                        Debug.LogError(
                            $"Impossible Map! No fitting module could be found for {cell}. solved Score: {cell.SolvedScore}",
                            gameObject);
                    }

                    if (cell.SolvedScore == 1)
                    {
                        if (cell._isCellSet)
                            orderedCells.RemoveFirst();
                        else
                        {
                            cell.SetModule(cell.possibleModules[0]);
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

                    var c = 0;
                    var mLength = cell.possibleModules.Count;
                    var i = Random.Range(0, cell.possibleModules.Count);

                    while (c < mLength)
                    {
                        i = ++i % mLength;

                        if (cell.RemoveModule(cell.possibleModules[i])) break;

                        ++c;
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

            DebugLogger.Log("Applying FinishLevel", DebugOutputLevels.All, debugOutputLevel, gameObject);

            // Add end constraints
            FinishLevel(ref cells);

            finishLevelStpwtch.Stop();

            stopwatch.Stop();

            DebugLogger.Log($"Applying initial constraints took {applyInitConstr.Elapsed.TotalMilliseconds}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            DebugLogger.Log(
                $"Applying finishing level took {finishLevelStpwtch.Elapsed.TotalMilliseconds}ms",
                DebugOutputLevels.Runtime, debugOutputLevel, gameObject);
            DebugLogger.Log(
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

        #region Grid-Methods

        /// <summary>
        /// Generates the three-dimensional grid.
        /// </summary>
        public void GenerateGrid()
        {
            var cellScale = cellPrefab.transform.localScale;

            if (dimensions.x > 0 && dimensions.y > 0 && dimensions.z > 0)
            {
                // Generate grid
                cells = new Cell[dimensions.x, dimensions.y, dimensions.z];

                var origin = transform.position;
                var bottomLeft = new Vector3(
                    origin.x - dimensions.x * cellScale.x / 2f + cellScale.x / 2f,
                    origin.y,
                    origin.z - dimensions.z * cellScale.z / 2f + cellScale.z / 2f
                );

                for (int x = 0; x < dimensions.x; x++)
                for (int y = 0; y < dimensions.y; y++)
                for (int z = 0; z < dimensions.z; z++)
                {
                    var curPos = new Vector3(
                        bottomLeft.x + x * cellScale.x,
                        bottomLeft.y + y * cellScale.y,
                        bottomLeft.z + z * cellScale.z
                    );

                    // Create new cell
                    var cellObj = Instantiate(cellPrefab, curPos, Quaternion.identity, gameObject.transform);
                    cellObj.name = $"({x}, {y}, {z})";
                    var cell = cellObj.GetComponent<Cell>();
                    cells[x, y, z] = cell;
                }

                // Assign neighbours for every cell
                for (int x = 0; x < dimensions.x; x++)
                for (int y = 0; y < dimensions.y; y++)
                for (int z = 0; z < dimensions.z; z++)
                {
                    var cell = cells[x, y, z];
                    for (int i = 0; i < 6; i++)
                    {
                        int nx = x, ny = y, nz = z;

                        // TODO: Make this cleaner with a loop and a condition over i
                        switch (i)
                        {
                            case 0:
                                nz++;
                                break;
                            case 1:
                                ny++;
                                break;
                            case 2:
                                nx++;
                                break;
                            case 3:
                                nz--;
                                break;
                            case 4:
                                ny--;
                                break;
                            case 5:
                                nx--;
                                break;
                        }

                        if (nx < 0 || ny < 0 || nz < 0 || nx > dimensions.x - 1 || ny > dimensions.y - 1 ||
                            nz > dimensions.z - 1)
                        {
                            // Outside of grid`s dimensions
                            cell.neighbourCells[i] = null;
                        }
                        else
                        {
                            cell.neighbourCells[i] = cells[nx, ny, nz];
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Impossible grid dimensions!", gameObject);
            }
        }

        /// <summary>
        /// Starts Wave-function-collapse algorithm
        /// </summary>
        public void GenerateLevel()
        {
            RemoveGrid();

            GenerateGrid();

            // Wave-function-collapse algorithm
            GenerateLevelWFC(ref cells, seed != -1 ? seed : Environment.TickCount);
        }

        /// <summary>
        /// Destroys the current grid.
        /// </summary>
        public void RemoveGrid()
        {
            var children = transform.Cast<Transform>().ToList();

            foreach (var child in children)
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
                        Destroy(child.gameObject);
#endif
            }
        }

        #endregion
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
}