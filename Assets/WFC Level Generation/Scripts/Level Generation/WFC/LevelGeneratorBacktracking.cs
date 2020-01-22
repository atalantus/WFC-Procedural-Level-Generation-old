using System.Collections.Generic;
using WFCLevelGeneration.Util;

namespace WFCLevelGeneration
{
    public class LevelGeneratorBacktracking : WFCBase
    {
        private bool RemoveModule(List<Cell> previousCells)
        {
            var sortedCells = new List<Cell>(previousCells);

            // sort cells
            sortedCells.Sort((c1, c2) => c1.SolvedScore.CompareTo(c2.SolvedScore));

            // get cell to change in this step
            while (sortedCells.Count > 0)
            {
                var cell = sortedCells[0];

                if (cell.SolvedScore > 1 || !cell.isCellSet)
                {
                    break;
                }

                // this cell is already set
                sortedCells.RemoveAt(0);
            }

            if (sortedCells.Count == 0)
            {
                // every cell is already set
                // we're done
                return true;
            }

            // try changing the chosen cell
            if (sortedCells[0].SolvedScore == 1)
            {
                // try setting this cell
                var success = sortedCells[0].SetLastModule();

                if (!success)
                {
                    cellHistories?.Add(new Cell.CellHistory(Cell.CellHistory.CellActions.Reset, sortedCells[0],
                        sortedCells[0].possibleModules[0]));
                    return false;
                }

                success = RemoveModule(sortedCells);

                if (!success)
                {
                    cellHistories?.Add(new Cell.CellHistory(Cell.CellHistory.CellActions.Reset, sortedCells[0],
                        sortedCells[0].possibleModules[0]));
                }

                return success;
            }

            // save cell states
            var sortedCellsStates = new Cell.CellState[sortedCells.Count];

            for (var i = 0; i < sortedCells.Count; i++)
            {
                sortedCellsStates[i] = new Cell.CellState(sortedCells[i]);
            }

            // try every cell sorted after their entropy until success
            foreach (var cell in sortedCells)
            {
                // remove modules from this cell until success
                var modules = new List<Module>(cell.possibleModules).ToArray();
                rng.Shuffle(modules);

                foreach (var module in modules)
                {
                    if (cell.RemoveModule(module))
                    {
                        var success = RemoveModule(sortedCells);

                        if (success) return true;
                    }

                    // removing this module didn't work
                    // reset cell states and try next module
                    for (var i = 0; i < sortedCells.Count; i++)
                    {
                        sortedCells[i].ResetCellState(sortedCellsStates[i]);
                    }
                }

                // no module of this cell works
                // try next cell in entropy sorted list
            }

            return false;
        }

        protected override bool CollapseWaveFunction()
        {
            var initialCells = new List<Cell>();

            foreach (var cell in cells)
            {
                initialCells.Add(cell);
            }

            return RemoveModule(initialCells);
        }
    }
}