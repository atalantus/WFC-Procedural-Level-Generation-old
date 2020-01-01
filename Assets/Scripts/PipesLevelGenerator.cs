using System.Collections;
using System.Collections.Generic;
using LevelGeneration.WFC;
using UnityEngine;

public class PipesLevelGenerator : LevelGenerator
{
    protected override void ApplyInitialConstraints()
    {
        StandardConstraints.WorldBordersConstraint(ref cells, 1, 0, 0, 0, 0, 0);
    }

    protected override void ApplyFinalConstraints()
    {
    }
}