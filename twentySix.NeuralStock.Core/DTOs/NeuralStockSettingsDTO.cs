namespace twentySix.NeuralStock.Core.DTOs
{
    using System;

    public class NeuralStockSettingsDTO
    {
        public int Id { get; set; }

        public double InitialCash { get; set; }

        public DateTime StartDate { get; set; }

        public double PercentageTraining { get; set; }

        public int NumberANNs { get; set; }

        public int NumberHiddenLayers { get; set; }

        public int NumberNeuronsHiddenLayer { get; set; }
    }
}