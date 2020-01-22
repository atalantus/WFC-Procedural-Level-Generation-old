using UnityEngine;

namespace WFCLevelGeneration.Constraints
{
    public class SetModuleConstraint : IGenerationConstraint
    {
        private readonly Cell _cell;
        private readonly Module _module;

        public SetModuleConstraint(Cell cell, Module module)
        {
            _cell = cell;
            _module = module;
        }

        public void Execute(ref Cell[,,] cells)
        {
            if (!_cell.SetModule(_module))
                Debug.LogError($"Error setting the Module {_module} in SetModuleConstraint at {_cell.name}");
        }
    }
}