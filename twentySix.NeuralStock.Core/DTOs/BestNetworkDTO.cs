namespace twentySix.NeuralStock.Core.DTOs
{
    using System;

    public class BestNetworkDTO
    {
        public int Id { get; set; }

        public int StockId { get; set; }

        public int StrategyId { get; set; }

        public DateTime Timestamp { get; set; }

        public double BuyLevel { get; set; }

        public double SellLevel { get; set; }

        public double[] TrainingMeansInput { get; set; }

        public double[] TrainingStdDevsInput { get; set; }

        public double[] TrainingMeansOutput { get; set; }

        public double[] TrainingStdDevsOutput { get; set; }

        public byte[] BestNeuralNet { get; set; }

        public string StrategySettings { get; set; }
    }
}