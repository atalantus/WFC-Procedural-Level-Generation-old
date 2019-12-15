﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace LevelGeneration
{
    /// <summary>
    /// Generates level using the wave-function-collapse algorithm
    /// </summary>
    public class LevelGenerator
    {
        private static LevelGenerator _instance;

        /// <summary>
        /// The Level Generator
        /// </summary>
        public static LevelGenerator Instance => _instance ?? (_instance = new LevelGenerator());

        /// <summary>
        /// Stores the cells in a heap having the closest cell to being solved as first element
        /// </summary>
        public Heap<Cell> OrderedCells;

        private LevelGenerator()
        {
        }

        /// <summary>
        /// Wave-function-collapse algorithm
        /// TODO: Could be multithreaded to increase performance
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        /// <param name="seed">RNG seed</param>
        public void GenerateLevelWFC(ref Cell[,] cells, int seed)
        {
            // Set RNG seed
            Random.InitState(seed);

            // Instantiate cells heap
            OrderedCells = new Heap<Cell>(cells.GetLength(0) * cells.GetLength(1));

            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    OrderedCells.Add(cells[i, j]);
                }
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Debug.LogWarning("Start Wave-function-collapse algorithm");

            // Make sure the level fits our initial constraints
            ApplyInitialConstraints(ref cells);

            // Wave-function-collapse Algorithm
            while (true)
            {
                //Debug.Log("Starting another iteration! Removing next module.");

                // Remove finished cells from heap
                while (OrderedCells.Count > 0)
                {
                    var cell = OrderedCells.GetFirst();

                    if (cell.SolvedScore == 1)
                    {
                        OrderedCells.RemoveFirst();
                    }
                    else
                    {
                        break;
                    }
                }

                // Remove random module from cell
                if (OrderedCells.Count > 0)
                {
                    var cell = OrderedCells.GetFirst();
                    cell.RemoveModule(cell.possibleModulesIndices[Random.Range(0, cell.possibleModulesIndices.Count)]);
                }
                else
                {
                    // Finished
                    break;
                }
            }

            stopwatch.Stop();
            Debug.LogWarning(
                $"Wave-function-collapse algorithm finished in {stopwatch.Elapsed.TotalMilliseconds}ms (Seed: {seed})");
        }

        /// <summary>
        /// Checks if the cells of the generated level matches with each other
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        /// <returns>List of not matching cells` (x, y)-coordinates</returns>
        public List<Tuple<int, int>> CheckGeneratedLevel(ref Cell[,] cells)
        {
            var notMatchingCells = new List<Tuple<int, int>>();

            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    var cell = cells[i, j];
                    var bCell = cell.neighbourCells[0];
                    var rCell = cell.neighbourCells[1];

                    var matchesNeighbours = true;

                    if (bCell != null)
                    {
                        if (ModuleManager.Instance.modules[cell.possibleModulesIndices[0]].faceConnections[0] !=
                            ModuleManager.Instance.modules[bCell.possibleModulesIndices[0]].faceConnections[2])
                        {
                            matchesNeighbours = false;
                            Debug.LogWarning($"CheckGeneratedLevel | ({i}, {j}) not matching with ({i}, {j + 1})");
                        }
                    }

                    if (rCell != null)
                    {
                        if (ModuleManager.Instance.modules[cell.possibleModulesIndices[0]].faceConnections[1] !=
                            ModuleManager.Instance.modules[rCell.possibleModulesIndices[0]].faceConnections[3])
                        {
                            matchesNeighbours = false;
                            Debug.LogWarning($"CheckGeneratedLevel | ({i}, {j}) not matching with ({i + 1}, {j})");
                        }
                    }

                    if (!matchesNeighbours) notMatchingCells.Add(new Tuple<int, int>(i, j));
                }
            }

            return notMatchingCells;
        }

        /// <summary>
        /// Resolve all initial constraints
        /// </summary>
        /// <param name="cells">The grid`s cells</param>
        private void ApplyInitialConstraints(ref Cell[,] cells)
        {
            Debug.LogWarning("Resolve initial constraints");
        }
    }
}