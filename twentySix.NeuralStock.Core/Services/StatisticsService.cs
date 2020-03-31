namespace twentySix.NeuralStock.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    using twentySix.NeuralStock.Core.Services.Interfaces;

    [Export(typeof(IStatisticsService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class StatisticsService : IStatisticsService
    {
        [ImportingConstructor]
        public StatisticsService()
        {
        }

        public double Mean(double[] data)
        {
            return data.Average();
        }

        public double StandardDeviation(double[] data)
        {
            double ret = 0;
            if (data.Any())
            {
                double avg = this.Mean(data);
                double sum = data.Sum(d => Math.Pow(d - avg, 2));
                ret = Math.Sqrt(sum / (data.Length - 1));
            }

            if (double.IsNaN(ret))
            {
                return 0;
            }

            return ret;
        }

        public double Median(double[] data)
        {
            var sortedArray = data.OrderBy(number => number).ToArray();
            var itemIndex = sortedArray.Length / 2;

            if (data.Length < 2)
            {
                return 0;
            }

            if (sortedArray.Length % 2 == 0)
            {
                return (sortedArray.ElementAt(itemIndex) + sortedArray.ElementAt(itemIndex - 1)) / 2;
            }

            return sortedArray.ElementAt(itemIndex);
        }

        public double Percentile(double[] data, double percentile)
        {
            if (data.Length < 2)
            {
                return data[0];
            }

            Array.Sort(data);
            double realIndex = percentile * (data.Length - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;

            if (index + 1 < data.Length)
            {
                return (data[index] * (1 - frac)) + (data[index + 1] * frac);
            }

            return data[index];
        }

        public Dictionary<string, int> Bucketize(double[] sourceOriginal, int totalBuckets)
        {
            if (sourceOriginal == null || sourceOriginal.Length < 2)
            {
                return null;
            }

            var source = sourceOriginal.ToList();

            var min = source.Min();
            var max = source.Max();

            if (Math.Abs(min - max) < 0.001)
            {
                return null;
            }

            var bucketSize = (max - min) / totalBuckets;
            var bucketsList = new int[totalBuckets];

            foreach (var value in source)
            {
                var bucketIndex = 0;
                if (bucketSize > 0.0)
                {
                    bucketIndex = (int)((value - min) / bucketSize);

                    if (bucketIndex == totalBuckets)
                    {
                        bucketIndex--;
                    }
                }

                bucketsList[bucketIndex]++;
            }

            return Enumerable
                .Range(0, bucketsList.Length)
                .ToDictionary(i => (min + (bucketSize * (i + 1))).ToString("C2"), i => bucketsList[i]);
        }
    }
}