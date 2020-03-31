namespace twentySix.NeuralStock.Core.Models
{
    using System;

    public class AnnDataPoint
    {
        public DateTime Date { get; set; }

        public double[] Inputs { get; set; }

        public double[] Outputs { get; set; }
    }
}