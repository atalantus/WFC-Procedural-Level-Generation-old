using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LevelGeneration
{
    /// <summary>
    /// Acts as placeholder for the modules possibility space.
    /// </summary>
    public class Cell : MonoBehaviour, IHeapItem<Cell>
    {
        #region Attributes

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
        /// <see cref="LevelGenerator"/>
        /// </summary>
        private LevelGenerator _levelGenerator;

        /// <summary>
        /// The adjacent cells (forward, up, right, back, down, left)
        /// Element can be null if the cell is on the grid`s edge
        /// </summary>
        public Cell[] neighbourCells = new Cell[6];

        /// <summary>
        /// Heap Index
        /// </summary>
        public int HeapIndex { get; set; }

        #endregion

        #region Methods

        private void Awake()
        {
            _levelGenerator = LevelGenerator.Instance;
            possibleModulesIndices = new List<int>();

            // At the beginning every module is possible
            for (int i = 0; i < _levelGenerator.modules.Count; i++)
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
            Util.DebugLog($"FilterCell({faceFilter.FaceIndex}, {faceFilter.FilterID.ToString()})",
                LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel, gameObject);

            if (SolvedScore == 1) return;

            var removingModules = new List<int>();

            // Filter possible Modules list for a given filter
            for (int i = 0; i < possibleModulesIndices.Count; i++)
            {
                var module = _levelGenerator.modules[possibleModulesIndices[i]];

                Util.DebugLog(
                    $"Checking {module.moduleGO.name} for face filter {faceFilter.FaceIndex}, {faceFilter.FilterID}",
                    LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel, gameObject);

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

            // Check if the cell has only one possible module left now
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
            var module = _levelGenerator.modules[moduleIndex];

            // Remove module from possibility space
            possibleModulesIndices.Remove(moduleIndex);

            // Update item on the heap
            _levelGenerator.orderedCells.UpdateItem(this);

            Util.DebugLog($"{gameObject.name} | RemoveModule({module.moduleGO.name})",
                LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel, gameObject);

            for (int j = 0; j < neighbourCells.Length; j++)
            {
                // Only check if cell actually has a neighbour on this face
                if (neighbourCells[j] == null) continue;

                var faceId = module.faceConnections[j];
                var lastWithFaceId = true;

                // Search in other possible modules for the same face id on the same face
                for (int i = 0; i < possibleModulesIndices.Count; i++)
                {
                    if (_levelGenerator.modules[possibleModulesIndices[i]].faceConnections[j] == faceId)
                    {
                        lastWithFaceId = false;
                        break;
                    }
                }

                if (lastWithFaceId)
                {
                    Util.DebugLog($"{gameObject.name} | Last face({j}, {faceId.ToString()})",
                        LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel, gameObject);

                    // Populate face changes to neighbour cell
                    var faceFilter = new FaceFilter(j, faceId);
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
            _levelGenerator.orderedCells.UpdateItem(this);
        }

        /// <summary>
        /// Force assigns this cell one specific module and automatically removes all other possibilities.
        /// </summary>
        /// <param name="moduleIndex">The module to assign</param>
        public void SetSpecialModule(int moduleIndex)
        {
            var removedFaceIds = new HashSet<int>[neighbourCells.Length];
            removedFaceIds.PopulateCollection();

            for (int i = 0; i < possibleModulesIndices.Count; i++)
            {
                if (i == moduleIndex) continue;
                var removedModule = _levelGenerator.modules[i];

                for (int j = 0; j < removedModule.faceConnections.Length; j++)
                {
                    removedFaceIds[j].Add(removedModule.faceConnections[j]);
                }
            }

            possibleModulesIndices = new List<int> {moduleIndex};
            var module = _levelGenerator.modules[moduleIndex];

            // Update item on the heap
            _levelGenerator.orderedCells.UpdateItem(this);

            // Propagate changes to neighbours
            for (int i = 0; i < neighbourCells.Length; i++)
            {
                if (neighbourCells[i] == null) continue;

                foreach (var faceId in removedFaceIds[i])
                {
                    // This face id was removed from this face
                    // Populate face changes to neighbour cell
                    var faceFilter = new FaceFilter(i, faceId);
                    neighbourCells[i].FilterCell(faceFilter);
                }
            }

            var go = Instantiate(module.moduleGO, transform.position,
                Quaternion.identity);
            go.transform.parent = transform;

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
            Util.DebugLog($"Set cell {name}!", LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel,
                gameObject);

            if (_isCellSet) return;

            var go = Instantiate(_levelGenerator.modules[possibleModulesIndices[0]].moduleGO,
                transform.position,
                Quaternion.identity);
            go.transform.parent = transform;

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

        #endregion
    }
}