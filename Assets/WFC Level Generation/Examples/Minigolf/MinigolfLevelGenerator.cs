using UnityEngine;
using WFCLevelGeneration.Constraints;

namespace WFCLevelGeneration.Examples
{
    public class MinigolfLevelGenerator : WFCBacktracking
    {
        private void Awake()
        {
            SetInitialConstraints(new BorderConstraint(null, null, 967653782, 967653782, 967653782, 967653782));
        }

        private void Start()
        {
            if (Application.isPlaying)
                GenerateLevel();
        }
    }
}