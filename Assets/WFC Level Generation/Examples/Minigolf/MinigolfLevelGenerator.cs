using UnityEngine;

namespace WFCLevelGeneration.Examples
{
    public class MinigolfLevelGenerator : LevelGenerator
    {
        protected override void ApplyInitialConstraints()
        {
            StandardConstraints.WorldBordersConstraint(ref cells,
                null,
                null,
                967653782,
                967653782,
                967653782,
                967653782);
        }

        private void Start()
        {
            if (Application.isPlaying)
                GenerateLevel();
        }
    }
}