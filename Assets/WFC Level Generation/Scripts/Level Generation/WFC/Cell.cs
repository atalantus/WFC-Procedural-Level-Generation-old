using System;
using System.Collections.Generic;
using WFCLevelGeneration.Util;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace WFCLevelGeneration
{
    /// <summary>
    /// Acts as placeholder for the modules possibility space.
    /// </summary>
    [ExecuteInEditMode]
    public class Cell : MonoBehaviour
    {
        #region Attributes

        /// <summary>
        /// Solved score
        /// </summary>
        public int SolvedScore => possibleModules.Count;

        /// <summary>
        /// The placed GameObject
        /// </summary>
        public GameObject placedModule;

        /// <summary>
        /// Was the module object already instantiated
        /// </summary>
        public bool isCellSet;

        /// <summary>
        /// Holds the still possible modules
        /// </summary>
        public List<Module> possibleModules;

        /// <summary>
        /// The adjacent cells (forward, up, right, back, down, left)
        /// Element can be null if the cell is on the grid`s edge
        /// </summary>
        public Cell[] neighbourCells = new Cell[6];

        [HideInInspector] public WFCBase levelGenerator;

        #endregion

        #region Methods

        /// <summary>
        /// Adds a given set of modules to the possibility space
        /// </summary>
        /// <param name="possibleModules">Modules indices to add</param>
        public void PopulateCell(Module[] possibleModules)
        {
            if (possibleModules.Length == 0) Debug.LogError($"Cell {name} was populated with zero possible modules!");
            this.possibleModules = new List<Module>(possibleModules.Length);

            for (var i = 0; i < possibleModules.Length; i++) this.possibleModules.Add(possibleModules[i]);
        }

        public void ResetCellState(CellState cellState)
        {
            // Reset cell on error state and backtrack!
            possibleModules = cellState.possibleModulesState;
            isCellSet = cellState.isCellSetState;
        }

        /// <summary>
        /// Filters a cell for a given face filter.
        /// </summary>
        /// <param name="faceFilter">Face filter</param>
        /// <param name="mustFit">When set to true filter all modules that do not fit the filter. When set to false it's the opposite.</param>
        public bool FilterCell(FaceFilter faceFilter, bool mustFit)
        {
            Debugger.Log($"FilterCell({faceFilter.ToString()}, {mustFit})",
                WFCBase.DebugOutputLevels.All, levelGenerator.debugOutputLevel, gameObject);

            // this cell is already set
            if (SolvedScore == 1) return true;

            // modules to remove by the given filter predicate
            var removingModules = new List<Module>();

            // Filter possible Modules list for the given filter
            for (var i = 0; i < possibleModules.Count; i++)
            {
                var module = possibleModules[i];

                var isImpossible = module.CheckModule(faceFilter);

                if (mustFit) isImpossible = !isImpossible;

                if (isImpossible)
                    // add module to remove list
                    removingModules.Add(possibleModules[i]);
            }

            // remove all filtered modules
            for (var i = 0; i < removingModules.Count; i++)
            {
                var success = RemoveModule(removingModules[i]);

                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the removing module had the last face type of any kind for this cell and if so populates the changes to the affected neighbour cell.
        /// Than removes module from <see cref="possibleModules"/>
        /// </summary>
        public bool RemoveModule(Module module)
        {
            if (possibleModules.Count == 1) return false;
            
            Debugger.Log($"RemoveModule({module.moduleGO.name})",
                WFCBase.DebugOutputLevels.All, levelGenerator.debugOutputLevel, gameObject);

            // Remove module from possibility space
            possibleModules.Remove(module);

            // Check for each neighbour
            for (var j = 0; j < neighbourCells.Length; j++)
            {
                if (neighbourCells[j] == null) continue;

                var faceId = module.faceConnections[j];
                var lastWithFaceId = true;

                // Search in other possible modules for the same face id on the same face direction
                for (var i = 0; i < possibleModules.Count; i++)
                    if (possibleModules[i].faceConnections[j] == faceId)
                    {
                        lastWithFaceId = false;
                        break;
                    }

                if (lastWithFaceId)
                {
                    // The removed module was the last with this specific face id for this face direction
                    // populate changes to facing neighbour
                    Debugger.Log($"Last face({j}, {faceId.ToString()})",
                        WFCBase.DebugOutputLevels.All, levelGenerator.debugOutputLevel, gameObject);

                    var faceFilter = new FaceFilter((FaceFilter.FaceDirections) j, faceId);

                    // populate face changes to neighbour cell
                    var success = neighbourCells[j].FilterCell(faceFilter, false);

                    if (!success)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Assigns this cell the first module and automatically removes all other possibilities.
        /// </summary>
        public bool SetLastModule()
        {
            var module = possibleModules[0];

            Debugger.Log("Set cell!", WFCBase.DebugOutputLevels.All, levelGenerator.debugOutputLevel,
                gameObject);

            // set cell state
            possibleModules = new List<Module> {module};
            isCellSet = true;

            // add "set" entry to cell histories
            levelGenerator.cellHistories?.Add(new CellHistory(CellHistory.CellActions.Set, this, module));

            // Check if it actually fits to already set neighbour cells
            for (var i = 0; i < neighbourCells.Length; i++)
            {
                if (neighbourCells[i] == null || !neighbourCells[i].isCellSet) continue;

                if (module.faceConnections[i] != neighbourCells[i].possibleModules[0].faceConnections[(i + 3) % 6])
                    return false;
            }

            // Propagate changes to neighbours
            for (var i = 0; i < neighbourCells.Length; i++)
            {
                if (neighbourCells[i] == null) continue;

                // This face id was chosen from this face
                // Populate face changes to neighbour cell
                var faceFilter = new FaceFilter((FaceFilter.FaceDirections) i, module.faceConnections[i]);
                var success = neighbourCells[i].FilterCell(faceFilter, true);

                if (!success) return false;
            }

            return true;
        }

        #endregion

        public struct CellState
        {
            public readonly List<Module> possibleModulesState;
            public readonly bool isCellSetState;

            public CellState(Cell cell)
            {
                this.possibleModulesState = new List<Module>(cell.possibleModules);
                this.isCellSetState = cell.isCellSet;
            }
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
                        var go = Instantiate(module.moduleGO, cell.transform.position,
                            module.moduleGO.transform.rotation);
                        go.transform.parent = cell.transform;
                        cell.placedModule = go;
                        break;
                    case CellActions.Reset:
                        if (Application.isPlaying) Destroy(cell.placedModule);
                        else DestroyImmediate(cell.placedModule);
                        break;
                }
            }
        }
    }
}