using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LevelGeneration
{
    /// <summary>
    /// Acts as placeholder for the modules possibility space
    /// </summary>
    public class Cell : MonoBehaviour, IHeapItem<Cell>
    {
        /// <summary>
        /// Solved score
        /// </summary>
        public int SolvedScore => possibleModulesIndices.Count;

        /// <summary>
        /// Was the module object already instantiated
        /// </summary>
        private bool _isCellSet;

        /// <summary>
        /// Holds the indices of the still possible modules
        /// </summary>
        public List<int> possibleModulesIndices;

        /// <summary>
        /// <see cref="ModuleManager"/>
        /// </summary>
        private ModuleManager _moduleManager;

        /// <summary>
        /// The neighbouring cells starting with the bottom one going counter clockwise (bottom, right, top, left)
        /// Can be null if cell is on the grid`s edge
        /// </summary>
        public Cell[] neighbourCells = new Cell[4];

        /// <summary>
        /// Heap Index
        /// </summary>
        public int HeapIndex { get; set; }

        private void Awake()
        {
            _moduleManager = ModuleManager.Instance;
            possibleModulesIndices = new List<int>();

            // At the beginning every module is possible
            for (int i = 0; i < _moduleManager.modules.Count; i++)
            {
                possibleModulesIndices.Add(i);
            }
        }

        /// <summary>
        /// Filters a cell for a given face filter
        /// </summary>
        /// <param name="faceFilter">Face filter</param>
        public void FilterCell(FaceFilter faceFilter)
        {
            //Debug.Log($"FilterCell({faceFilter.FaceIndex}, {faceFilter.FilterType.ToString()})", gameObject);

            if (SolvedScore == 1) return;

            var removingModules = new List<int>();

            // Filter possible Modules list for a given filter
            for (int i = 0; i < possibleModulesIndices.Count; i++)
            {
                var module = _moduleManager.modules[possibleModulesIndices[i]];
                var isImpossible = module.CheckModule(faceFilter);

                if (isImpossible)
                {
                    // Remove module
                    removingModules.Add(possibleModulesIndices[i]);
                }
            }

            // Now remove filtered modules
            for (int i = 0; i < removingModules.Count; i++)
            {
                RemoveModule(removingModules[i]);
            }

            // Check if the cell has only one possible module left
            CheckSetCell();
        }

        /// <summary>
        /// Checks if the removing module had the last face type of any kind for this cell and if so populates the changes to the affected neighbour cell.
        /// Than removes module from <see cref="possibleModulesIndices"/>
        /// </summary>
        /// <param name="moduleIndex">Index of the removing module in <see cref="ModuleManager.modules"/></param>
        public void RemoveModule(int moduleIndex)
        {
            // Check module`s face types
            var module = _moduleManager.modules[moduleIndex];

            // Remove module from possibility space
            possibleModulesIndices.Remove(moduleIndex);

            // Update item on the heap
            LevelGenerator.Instance.OrderedCells.UpdateItem(this);

            //Debug.Log($"{gameObject.name} | RemoveModule({module.moduleGO.name})", gameObject);

            for (int j = 0; j < 4; j++)
            {
                // Only check if cell has a neighbour on this face
                if (neighbourCells[j] == null) continue;

                var faceType = module.faceConnections[j];
                var lastFaceType = true;

                // Search in other possible modules for the same face type
                for (int i = 0; i < possibleModulesIndices.Count; i++)
                {
                    if (_moduleManager.modules[possibleModulesIndices[i]].faceConnections[j] == faceType)
                    {
                        lastFaceType = false;
                        break;
                    }
                }

                if (lastFaceType)
                {
                    //Debug.Log($"{gameObject.name} | Last face({j}, {faceType.ToString()})", gameObject);

                    // Populate face changes to neighbour cell
                    var faceFilter = new FaceFilter(j, faceType);
                    neighbourCells[j].FilterCell(faceFilter);
                }
            }

            CheckSetCell();
        }

        /// <summary>
        /// Removes a module without populating possible changes to neighbouring cells
        /// </summary>
        /// <param name="moduleIndex">Index of the removing module in <see cref="ModuleManager.modules"/></param>
        public void SimpleRemoveModule(int moduleIndex)
        {
            // Remove module from possibility space
            possibleModulesIndices.Remove(moduleIndex);

            // Update item on the heap
            LevelGenerator.Instance.OrderedCells.UpdateItem(this);
        }

        /// <summary>
        /// Force assigns this cell one specific module and automatically removes all other possibilities.
        /// </summary>
        /// <param name="moduleIndex">The module to assign</param>
        public void SetSpecialModule(int moduleIndex)
        {
            possibleModulesIndices = new List<int> {moduleIndex};

            var module = ModuleManager.Instance.modules[moduleIndex];

            // Update item on the heap
            LevelGenerator.Instance.OrderedCells.UpdateItem(this);

            var totalFaceTypes = ModuleManager.Instance.moduleConnections.faceConnectionsMap.Count;

            // Propagate changes to neighbours
            for (int i = 0; i < 4; i++)
            {
                if (neighbourCells[i] == null) continue;

                for (int faceType = 0; faceType < totalFaceTypes; faceType++)
                {
                    if (faceType != module.faceConnections[i])
                    {
                        // This face type was removed from this face
                        // Populate face changes to neighbour cell
                        var faceFilter = new FaceFilter(i, faceType);
                        neighbourCells[i].FilterCell(faceFilter);
                    }
                }
            }

            var newModule = Instantiate(module.moduleGO, transform.position,
                Quaternion.identity, transform);

            _isCellSet = true;
        }

        /// <summary>
        /// Checks if the cell is solved
        /// </summary>
        private void CheckSetCell()
        {
            // Only set cell if one final module is left
            if (SolvedScore == 1) SetCell();
            else if (SolvedScore <= 0)
                Debug.LogError($"Impossible Map! No fitting module could be found. solvedScore: {SolvedScore}",
                    gameObject);
        }

        /// <summary>
        /// Assigns the final module to the cell
        /// </summary>
        private void SetCell()
        {
            //Debug.Log("Set cell!", gameObject);

            if (_isCellSet) return;

            var newModule = Instantiate(ModuleManager.Instance.modules[possibleModulesIndices[0]].moduleGO,
                transform.position,
                Quaternion.identity, transform);

            _isCellSet = true;
        }

        /// <summary>
        /// Compares two cells using their solved score
        /// </summary>
        /// <param name="other">Cell to compare</param>
        /// <returns></returns>
        public int CompareTo(Cell other)
        {
            var compare = SolvedScore.CompareTo(other.SolvedScore);
            if (compare == 0)
            {
                var r = Random.Range(1, 3);
                return r == 1 ? -1 : 1;
            }

            return -compare;
        }
    }
}