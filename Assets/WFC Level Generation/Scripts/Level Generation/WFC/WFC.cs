﻿using System.Collections.Generic;

namespace WFCLevelGeneration
{
    public class WFC : WFCBase
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
                    if (cell.isCellSet)
                    {
                        orderedCells.RemoveAt(0);
                    }
                    else
                    {
                        if (!cell.SetLastModule()) return false;
                    }
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

            foreach (var cell in cells)
            {
                orderedCells.Add(cell);
            }

            for (var i = 0; i < retryCount; i++)
            {
                if (ResolveLevel(orderedCells)) return true;
                // TODO: Reset orderedCells
            }

            return false;
        }
    }
}