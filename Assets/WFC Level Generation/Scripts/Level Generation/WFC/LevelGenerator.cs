using System.Collections.Generic;
using UnityEngine;

namespace WFCLevelGeneration
{
    public class LevelGenerator : WFCBase
    {
        public int retryCount = 5;

        private bool ResolveLevel(List<Cell> orderedCells)
        {
            // TODO: Use Min-Heap instead of sorting list
            while (orderedCells.Count > 0)
            {
                orderedCells.Sort((c1, c2) => c1.SolvedScore.CompareTo(c2.SolvedScore));
                var cell = orderedCells[0];

                if (cell.SolvedScore == 1)
                {
                    if (cell.isCellSet) orderedCells.RemoveAt(0);
                    else if (!cell.SetLastModule()) return false;
                }
                else
                {
                    if (!cell.RemoveModule(cell.possibleModules[rng.Next(0, cell.possibleModules.Count)])) return false;
                }
            }

            return true;
        }

        protected override bool CollapseWaveFunction()
        {
            var orderedCells = new List<Cell>();
            var savedCellStates = new List<Cell.CellState>();

            foreach (var cell in cells)
            {
                orderedCells.Add(cell);
                savedCellStates.Add(new Cell.CellState(cell));
            }

            for (var i = 0; i < retryCount; i++)
            {
                // Reset cell histories
                cellHistories = new List<Cell.CellHistory>();

                if (ResolveLevel(new List<Cell>(orderedCells))) return true;

                // Reset orderedCells
                for (var j = 0; j < orderedCells.Count; j++)
                {
                    orderedCells[j].ResetCellState(savedCellStates[j]);
                }

                if (i == retryCount - 1) Debug.LogError($"Couldn't find solution in {i + 1} tries.");
            }

            return false;
        }
    }
}