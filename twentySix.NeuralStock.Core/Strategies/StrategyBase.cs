namespace twentySix.NeuralStock.Core.Strategies
{
    using System.Collections.Generic;
    using System.Linq;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    public abstract class StrategyBase : IStrategy
    {
        public abstract int Id { get; }

        public abstract StrategySettings Settings { get; protected set; }

        public double[] TrainingMeansInput { get; set; }

        public double[] TrainingMeansOutput { get; set; }

        public double[] TrainingStdDevsInput { get; set; }

        public double[] TrainingStdDevsOutput { get; set; }

        protected IDataProcessorService DataProcessorService { get; set; }

        protected IStatisticsService StatisticsService { get; set;  }

        public List<AnnDataPoint> GetAnnData(HistoricalData historicalData, bool recalculateMeans = true)
        {
            var rawAnnDataPoints = this.GetRawAnnDataPoints(historicalData);
            var numberOfInputs = rawAnnDataPoints.First().Inputs.Length;
            var numberOfOutputs = rawAnnDataPoints.First().Outputs.Length;

            // calculate means and stddevs if we are dea
            if (recalculateMeans)
            {
                this.TrainingMeansInput = new double[numberOfInputs];
                this.TrainingStdDevsInput = new double[numberOfInputs];
                this.TrainingMeansOutput = new double[numberOfOutputs];
                this.TrainingStdDevsOutput = new double[numberOfOutputs];

                for (int i = 0; i < numberOfInputs; i++)
                {
                    this.TrainingMeansInput[i] = this.StatisticsService.Mean(rawAnnDataPoints.Select(x => x.Inputs[i]).ToArray());
                    this.TrainingStdDevsInput[i] = this.StatisticsService.StandardDeviation(rawAnnDataPoints.Select(x => x.Inputs[i]).ToArray());
                }

                for (int i = 0; i < numberOfOutputs; i++)
                {
                    this.TrainingMeansOutput[i] = this.StatisticsService.Mean(rawAnnDataPoints.Select(x => x.Outputs[i]).ToArray());
                    this.TrainingStdDevsOutput[i] = this.StatisticsService.StandardDeviation(rawAnnDataPoints.Select(x => x.Outputs[i]).ToArray());
                }
            }

            return this.Normalize(rawAnnDataPoints);
        }

        protected abstract IList<AnnDataPoint> GetRawAnnDataPoints(HistoricalData historicalData);

        private List<AnnDataPoint> Normalize(IList<AnnDataPoint> data)
        {
            var result = new List<AnnDataPoint>();
            var numberOfInputs = data.First().Inputs.Length;
            var numberOfOutputs = data.First().Outputs.Length;

            foreach (var annPoint in data)
            {
                result.Add(new AnnDataPoint
                {
                    Date = annPoint.Date,
                    Inputs = Enumerable.Range(0, numberOfInputs).Select(i => this.DataProcessorService.Normalize(annPoint.Inputs[i], this.TrainingMeansInput[i], this.TrainingStdDevsInput[i])).ToArray(),
                    Outputs = Enumerable.Range(0, numberOfOutputs).Select(i => this.DataProcessorService.Normalize(annPoint.Outputs[i], this.TrainingMeansOutput[i], this.TrainingStdDevsOutput[i])).ToArray()
                });
            }

            return result;
        }
    }
}