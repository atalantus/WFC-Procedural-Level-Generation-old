using System.Linq;
using UnityEngine;

namespace WFCLevelGeneration
{
    [ExecuteInEditMode]
    public class GridGenerator : MonoBehaviour
    {
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
        [Space(15)] [Header("Debugging")] public Cell[,,] cells;

        /// <summary>
        /// Generates the three-dimensional grid.
        /// </summary>
        protected void GenerateGrid()
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

                for (var x = 0; x < dimensions.x; x++)
                for (var y = 0; y < dimensions.y; y++)
                for (var z = 0; z < dimensions.z; z++)
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
                for (var x = 0; x < dimensions.x; x++)
                for (var y = 0; y < dimensions.y; y++)
                for (var z = 0; z < dimensions.z; z++)
                {
                    var cell = cells[x, y, z];
                    for (var i = 0; i < 6; i++)
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
                            // Outside of grid`s dimensions
                            cell.neighbourCells[i] = null;
                        else
                            cell.neighbourCells[i] = cells[nx, ny, nz];
                    }
                }
            }
            else
            {
                Debug.LogError("Impossible grid dimensions!", gameObject);
            }
        }

        /// <summary>
        /// Destroys the current grid.
        /// </summary>
        protected void RemoveGrid()
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
    }
}