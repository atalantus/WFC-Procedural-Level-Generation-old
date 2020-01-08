namespace WFCLevelGeneration.Examples
{
    public class MinigolfLevelGenerator : LevelGenerator
    {
        protected override void ApplyInitialConstraints()
        {
            StandardConstraints.WorldBordersConstraint(ref cells, 1, 0, 0, 0, 0, 0);
        }
    }
}