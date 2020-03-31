namespace twentySix.NeuralStock.Core.Strategies
{
    using System.Collections.Generic;

    using twentySix.NeuralStock.Core.Models;

    public interface IStrategy
    {
        int Id { get; }

        StrategySettings Settings { get; }

        double[] TrainingMeansInput { get; set; }

        double[] TrainingMeansOutput { get; set; }

        double[] TrainingStdDevsInput { get; set; }

        double[] TrainingStdDevsOutput { get; set; }

        List<AnnDataPoint> GetAnnData(HistoricalData historical, bool recalculateMeans = true);
    }
}