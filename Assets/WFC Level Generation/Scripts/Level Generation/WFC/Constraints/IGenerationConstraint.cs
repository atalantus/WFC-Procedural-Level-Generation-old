namespace WFCLevelGeneration.Constraints
{
    public interface IGenerationConstraint
    {
        void Execute(ref Cell [,,] cells);
    }
}