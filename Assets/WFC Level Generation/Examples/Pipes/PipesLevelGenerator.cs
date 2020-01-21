using UnityEngine;
using WFCLevelGeneration.Constraints;

namespace WFCLevelGeneration.Examples
{
    public class PipesLevelGenerator : WFC
    {
        private void Awake()
        {
            SetInitialConstraints(new BorderConstraint(1, 0, 0, 0, 0, 0));
        }

        private void Start()
        {
            if (Application.isPlaying)
                GenerateLevel();
        }
    }
}