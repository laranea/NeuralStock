namespace twentySix.NeuralStock.Core.DTOs
{
    using System;

    public class HistoricalDataDTO
    {
        public int Id { get; set; }

        public DateTime[] DateArray { get; set; }

        public double[] OpenArray { get; set; }

        public double[] CloseArray { get; set; }

        public double[] AdjCloseArray { get; set; }

        public double[] LowArray { get; set; }

        public double[] HighArray { get; set; }

        public double[] VolumeArray { get; set; }

        public double[] DividendArray { get; set; }
    }
}