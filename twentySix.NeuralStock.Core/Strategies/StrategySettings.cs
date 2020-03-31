namespace twentySix.NeuralStock.Core.Strategies
{
    using System;

    using Newtonsoft.Json;

    using twentySix.NeuralStock.Core.Extensions;

    public class StrategySettings
    {
        private static readonly Random RandomGenerator = new Random();

        public StrategySettings()
        {
            this.FwdDays = RandomGenerator.NextGaussianPositiveInteger(22, 5);

            this.PercentageChangeHigh = RandomGenerator.NextGaussianPositive(3d, 0.5);
            this.PercentageChangeLow = RandomGenerator.NextGaussian(-1d, -0.3d);

            this.MovingAverageCloseFast = RandomGenerator.NextGaussianPositiveInteger(21, 4);
            this.MovingAverageCloseSlow = RandomGenerator.NextGaussianPositiveInteger(63, 4);
            this.MovingAverageHighFast = RandomGenerator.NextGaussianPositiveInteger(32, 4);

            this.CCI = RandomGenerator.NextGaussianPositiveInteger(5, 4);
            this.RSI = RandomGenerator.NextGaussianPositiveInteger(10, 4);
            this.RSI2 = RandomGenerator.NextGaussianPositiveInteger(14, 4);

            this.MacdFast = RandomGenerator.NextGaussianPositiveInteger(3, 4);
            this.MacdSlow = RandomGenerator.NextGaussianPositiveInteger(30, 4);
            this.MacdSignal = RandomGenerator.NextGaussianPositiveInteger(32, 8);

            this.Hv1 = RandomGenerator.NextGaussianPositiveInteger(43, 4);

            this.FitClose = RandomGenerator.NextGaussianPositiveInteger(10, 4);
            this.FitOfFit = RandomGenerator.NextGaussianPositiveInteger(10, 4);

            this.RSI1Fit = RandomGenerator.NextGaussianPositiveInteger(11, 4);
            this.RSI2Fit = RandomGenerator.NextGaussianPositiveInteger(9, 4);
        }

        public int FwdDays { get; set; }

        public double PercentageChangeHigh { get; set; }

        public double PercentageChangeLow { get; set; }

        public int MovingAverageCloseFast { get; set; }

        public int MovingAverageCloseSlow { get; set; }

        public int MovingAverageHighFast { get; set; }

        public int CCI { get; set; }

        public int RSI { get; set; }

        public int RSI2 { get; set; }

        public int FitClose { get; set; }

        public int FitOfFit { get; set; }

        public int MacdFast { get; set; }

        public int MacdSlow { get; set; }

        public int MacdSignal { get; set; }

        public int Hv1 { get; set; }

        public int RSI1Fit { get; set; }

        public int RSI2Fit { get; set; }

        public static StrategySettings FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<StrategySettings>(json);
            }
            catch
            {
                return new StrategySettings();
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}