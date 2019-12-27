using System;
using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Controls the grid
    /// </summary>
    public class Grid : MonoBehaviour
    {
        /// <summary>
        /// Grid dimensions
        /// </summary>
        [Header("Options")] [Tooltip("The dimensions of the grid")]
        public Vector3Int size;

        /// <summary>
        /// Cell prefab
        /// </summary>
        [Tooltip("The cell prefab")] public GameObject cellPrefab;

        /// <summary>
        /// RNG seed
        /// </summary>
        [Tooltip("The generation seed. -1 means a random seed will be chosen.")]
        public int seed = -1;

        /// <summary>
        /// Cells matrix ([width, height, depth])
        /// </summary>
        [HideInInspector] public Cell[,,] cells;

        /// <summary>
        /// <see cref="LevelGenerator"/>
        /// </summary>
        private LevelGenerator _levelGenerator;

        private void Awake()
        {
            _levelGenerator = LevelGenerator.Instance;

            GenerateGrid();
        }

        private void Start()
        {
            GenerateLevel();
        }

        /// <summary>
        /// Generates the three-dimensional grid.
        /// </summary>
        public void GenerateGrid()
        {
            var cellScale = cellPrefab.transform.localScale;
            
            if (size.x > 0 && size.y > 0 && size.z > 0)
            {
                // Generate grid
                cells = new Cell[size.x, size.y, size.z];

                var origin = transform.position;
                var bottomLeft = new Vector3(
                    origin.x - size.x * cellScale.x / 2f + cellScale.x / 2f,
                    origin.y,
                    origin.z - size.z * cellScale.z / 2f + cellScale.z / 2f
                );

                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++)
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
                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++)
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

                        if (nx < 0 || ny < 0 || nz < 0 || nx > size.x - 1 || ny > size.y - 1 || nz > size.z - 1)
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
            // Wave-function-collapse algorithm
            _levelGenerator.GenerateLevelWFC(ref cells, seed != -1 ? seed : Environment.TickCount);
        }

        /// <summary>
        /// Destroys the current grid.
        /// </summary>
        public void RemoveGrid()
        {
            foreach (Transform child in gameObject.transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Checks if the grid is valid.
        /// </summary>
        /// <returns>true if the grid is valid</returns>
        public bool CheckGrid()
        {
            return _levelGenerator.CheckGeneratedLevel(ref cells);
        }
    }
}