namespace WFCLevelGeneration.Constraints
{
    public interface IWFCConstraint
    {
        void Execute(Cell [,,] cells);
    }
}