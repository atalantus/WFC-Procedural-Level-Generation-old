namespace WFCLevelGeneration.Constraints
{
    public interface IGenerationConstraint
    {
        void Execute(Cell [,,] cells);
    }
}