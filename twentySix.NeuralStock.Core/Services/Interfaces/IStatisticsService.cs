namespace twentySix.NeuralStock.Core.Services.Interfaces
{
    using System.Collections.Generic;

    public interface IStatisticsService
    {
        double Mean(double[] data);

        double StandardDeviation(double[] data);

        double Median(double[] data);

        double Percentile(double[] data, double percentile);

        Dictionary<string, int> Bucketize(double[] sourceOriginal, int totalBuckets);
    }
}