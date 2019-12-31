using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LevelGeneration
{
    /// <summary>
    /// Acts as placeholder for the modules possibility space.
    /// </summary>
    [ExecuteInEditMode]
    public class Cell : MonoBehaviour, IHeapItem<Cell>
    {
        #region Attributes

        /// <summary>
        /// Solved score
        /// </summary>
        public int SolvedScore => possibleModules.Count;

        /// <summary>
        /// Was the module object already instantiated
        /// </summary>
        public bool _isCellSet;

        /// <summary>
        /// Holds the still possible modules
        /// </summary>
        public List<Module> possibleModules;

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
        }

        /// <summary>
        /// Adds a given set of modules to the possibility space
        /// </summary>
        /// <param name="possibleModules">Modules indices to add</param>
        public void PopulateCell(Module[] possibleModules)
        {
            if (possibleModules.Length == 0) Debug.LogError($"Cell {name} was populated with zero possible modules!");
            this.possibleModules = new List<Module>(possibleModules.Length);

            for (int i = 0; i < possibleModules.Length; i++)
            {
                this.possibleModules.Add(possibleModules[i]);
            }
        }

        /// <summary>
        /// Filters a cell for a given face filter.
        /// </summary>
        /// <param name="faceFilter">Face filter</param>
        /// <param name="mustFit">When set to true filter all modules that do not fit the filter. When set to false it's the opposite.</param>
        public void FilterCell(FaceFilter faceFilter, bool mustFit)
        {
            Util.DebugLog($"FilterCell({faceFilter.ToString()}, {mustFit})",
                LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel, gameObject);

            if (SolvedScore == 1) return;

            var removingModules = new List<Module>();

            // Filter possible Modules list for the given filter
            for (int i = 0; i < possibleModules.Count; i++)
            {
                var module = possibleModules[i];

                Util.DebugLog(
                    $"Checking {module.moduleGO.name} for face filter {faceFilter.ToString()} with \"mustFit\": {mustFit}",
                    LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel, gameObject);

                var isImpossible = module.CheckModule(faceFilter);

                if (mustFit) isImpossible = !isImpossible;

                if (isImpossible)
                {
                    // Remove module
                    removingModules.Add(possibleModules[i]);
                }
            }

            // Now remove filtered modules
            for (int i = 0; i < removingModules.Count; i++)
            {
                RemoveModule(removingModules[i]);
            }
        }

        /// <summary>
        /// Checks if the removing module had the last face type of any kind for this cell and if so populates the changes to the affected neighbour cell.
        /// Than removes module from <see cref="possibleModules"/>
        /// </summary>
        /// <param name="moduleIndex">Index of the removing module in <see cref="ModuleManager.modules"/></param>
        public void RemoveModule(Module module)
        {
            Util.DebugLog($"{gameObject.name} | RemoveModule({module.moduleGO.name})",
                LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel, gameObject);

            // Check if removing this module will leave only one possible module option
            if (SolvedScore == 2 && !_isCellSet)
            {
                var possModCopy = new List<Module>(possibleModules);
                possModCopy.Remove(module);

                // Removing this module will set this cell
                SetModule(possModCopy[0]);
            }
            else
            {
                // Remove module from possibility space
                possibleModules.Remove(module);

                // Update item on the heap
                _levelGenerator.orderedCells.UpdateItem(this);

                for (int j = 0; j < neighbourCells.Length; j++)
                {
                    // Only check if cell actually has a neighbour on this face
                    if (neighbourCells[j] == null) continue;

                    var faceId = module.faceConnections[j];
                    var lastWithFaceId = true;

                    // Search in other possible modules for the same face id on the same face
                    for (int i = 0; i < possibleModules.Count; i++)
                    {
                        if (possibleModules[i].faceConnections[j] == faceId)
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
                        neighbourCells[j].FilterCell(faceFilter, false);
                    }
                }
            }
        }

        /// <summary>
        /// Assigns this cell one specific module and automatically removes all other possibilities.
        /// </summary>
        /// <param name="moduleIndex">The module to assign</param>
        public void SetModule(Module module)
        {
            Util.DebugLog($"Set cell {name}!", LevelGenerator.DebugOutputLevels.All, _levelGenerator.debugOutputLevel,
                gameObject);

            possibleModules = new List<Module> {module};

            // Update item on the heap
            _levelGenerator.orderedCells.UpdateItem(this);

            // Instantiate module game object
            var go = Instantiate(module.moduleGO, transform.position,
                Quaternion.identity);
            go.transform.parent = transform;

            _isCellSet = true;

            // Propagate changes to neighbours
            for (int i = 0; i < neighbourCells.Length; i++)
            {
                if (neighbourCells[i] == null) continue;

                // This face id was chosen from this face
                // Populate face changes to neighbour cell
                var faceFilter = new FaceFilter(i, module.faceConnections[(i + 3) % 6]);
                neighbourCells[i].FilterCell(faceFilter, true);
            }
        }

        /// <summary>
        /// Removes a module without populating possible changes to neighbouring cells
        /// </summary>
        /// <param name="moduleIndex">Index of the removing module in <see cref="ModuleManager.modules"/></param>
        public void SimpleRemoveModule(Module module)
        {
            // Remove module from possibility space
            possibleModules.Remove(module);

            // Update item on the heap
            _levelGenerator.orderedCells.UpdateItem(this);
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