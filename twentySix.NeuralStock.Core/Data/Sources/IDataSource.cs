namespace twentySix.NeuralStock.Core.Data.Sources
{
    using twentySix.NeuralStock.Core.Models;

    public interface IDataSource
    {
        string Name { get; }

        string GetSymbol(Stock symbol);
    }
}