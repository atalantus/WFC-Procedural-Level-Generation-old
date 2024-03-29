﻿using UnityEngine;

namespace WFCLevelGeneration.Constraints
{
    /// <summary>
    /// Adjacency constraints to cells lying on the world's borders.
    /// </summary>
    public class BorderConstraint : IGenerationConstraint
    {
        private readonly int?[] _borders;

        /// <summary>
        /// Creates a new <see cref="BorderConstraint"/>
        /// </summary>
        /// <param name="floorCnstr">Adjacency constraint for the floor. "null" means no constraint.</param>
        /// <param name="ceilingCnstr">Adjacency constraint for the ceiling. "null" means no constraint.</param>
        /// <param name="backCnstr">Adjacency constraint for the back border. "null" means no constraint.</param>
        /// <param name="forwardCnstr">Adjacency constraint for the forward border. "null" means no constraint.</param>
        /// <param name="rightCnstr">Adjacency constraint for the right border. "null" means no constraint.</param>
        /// <param name="leftConstr">Adjacency constraint for the left border. "null" means no constraint.</param>
        public BorderConstraint(int? floorCnstr = null, int? ceilingCnstr = null, int? backCnstr = null,
            int? forwardCnstr = null,
            int? rightCnstr = null, int? leftConstr = null)
        {
            _borders = new[] {floorCnstr, ceilingCnstr, backCnstr, forwardCnstr, rightCnstr, leftConstr};
        }

        public void Execute(ref Cell[,,] cells)
        {
            if (_borders == null || _borders.Length != 6) Debug.LogError("Border Constraints not set up correctly!");

            // apply floor and ceiling constraints
            for (var i = 0; i < 2; i++)
            {
                if ((i == 0 ? _borders[0] : _borders[1]) == null) continue;

                var y = (cells.GetLength(1) - 1) * i;
                for (var x = 0; x < cells.GetLength(0); x++)
                for (var z = 0; z < cells.GetLength(2); z++)
                {
                    var c = cells[x, y, z];
                    if (!c.FilterCell(
                        new FaceFilter(i == 0 ? FaceFilter.FaceDirections.Up : FaceFilter.FaceDirections.Down,
                            i == 0 ? _borders[0].Value : _borders[1].Value), true))
                        Debug.LogError(
                            $"Error resolving {(i == 0 ? "Floor" : "Ceiling")} in WorldBordersConstraint at {c.name}");
                }
            }

            // apply forward and back constraints    
            for (var i = 0; i < 2; i++)
            {
                if ((i == 0 ? _borders[2] : _borders[3]) == null) continue;

                var z = (cells.GetLength(2) - 1) * i;
                for (var x = 0; x < cells.GetLength(0); x++)
                for (var y = 0; y < cells.GetLength(1); y++)
                {
                    var c = cells[x, y, z];
                    if (!c.FilterCell(
                        new FaceFilter(i == 0 ? FaceFilter.FaceDirections.Forward : FaceFilter.FaceDirections.Back,
                            i == 0 ? _borders[2].Value : _borders[3].Value), true))
                        Debug.LogError(
                            $"Error resolving {(i == 0 ? "Back" : "Forward")} in WorldBordersConstraint at {c.name}");
                }
            }

            // apply right and left constraints    
            for (var i = 0; i < 2; i++)
            {
                if ((i == 0 ? _borders[4] : _borders[5]) == null) continue;

                var x = (cells.GetLength(0) - 1) * i;
                for (var y = 0; y < cells.GetLength(1); y++)
                for (var z = 0; z < cells.GetLength(2); z++)
                {
                    var c = cells[x, y, z];
                    if (!c.FilterCell(
                        new FaceFilter(i == 0 ? FaceFilter.FaceDirections.Right : FaceFilter.FaceDirections.Left,
                            i == 0 ? _borders[4].Value : _borders[5].Value), true))
                        Debug.LogError(
                            $"Error resolving {(i == 0 ? "Left" : "Right")} in WorldBordersConstraint at {c.name}");
                }
            }
        }
    }
}