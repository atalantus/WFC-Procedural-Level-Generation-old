using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Controls the grid
    /// </summary>
    public class Grid : MonoBehaviour
    {
        /// <summary>
        /// Grid sizes
        /// </summary>
        public Vector3Int size;

        /// <summary>
        /// Grid cell scales
        /// </summary>
        public Vector3Int cellScale;

        /// <summary>
        /// Cell prefab
        /// </summary>
        public GameObject cellPrefab;

        /// <summary>
        /// Cells matrix ([width, height, depth])
        /// </summary>
        public Cell[,,] cells;

        /// <summary>
        /// <see cref="LevelGenerator"/>
        /// </summary>
        private LevelGenerator _levelGenerator;

        /// <summary>
        /// RNG seed
        /// </summary>
        public int seed;

        private void Awake()
        {
            // TODO
            //_levelGenerator = LevelGenerator.Instance;

            GenerateGrid();

            // Wave-function-collapse algorithm#
            // TODO
            //_levelGenerator.GenerateLevelWFC(ref cells, seed != -1 ? seed : Environment.TickCount);
        }

        /// <summary>
        /// Generates the three-dimensional grid
        /// </summary>
        public void GenerateGrid()
        {
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
                var curPos = bottomLeft;

                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++)
                {
                    curPos = new Vector3(
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

                Debug.Break();

                // Assign neighbours for every cell
                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++)
                {
                    var cell = cells[x, y, z];
                    for (int i = 0; i < 6; i++)
                    {
                        int nx = x, ny = y, nz = z;

                        switch (i)
                        {
                            case 0:
                                nz++;
                                break;
                            case 1:
                                nx++;
                                break;
                            case 2:
                                nz--;
                                break;
                            case 3:
                                nx--;
                                break;
                            case 4:
                                ny++;
                                break;
                            case 5:
                                ny--;
                                break;
                        }

                        if (nx < 0 || ny < 0 || nz < 0 || nx > size.x - 1 || ny > size.y - 1 || nz > size.z - 1)
                        {
                            // Outside of grid`s dimensions
                            // TODO
                            //cell.neighbourCells[l] = null;
                        }
                        else
                        {
                            // TODO
                            //cell.neighbourCells[l] = cells[x, z, k];
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
        /// Destroys the current grid
        /// </summary>
        public void RemoveGrid()
        {
            foreach (Transform child in gameObject.transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Checks if the grid is valid
        /// </summary>
        /// <returns>true if the grid is valid</returns>
        public bool CheckGrid()
        {
            // TODO
            //var notMatchingCells = _levelGenerator.CheckGeneratedLevel(ref cells);

            //return notMatchingCells.Count == 0;
            return false;
        }
    }
}