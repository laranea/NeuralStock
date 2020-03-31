namespace twentySix.NeuralStock.Core.Services.Interfaces
{
    using System;
    using System.Collections.Generic;

    using twentySix.NeuralStock.Core.Models;

    public interface IDataProcessorService
    {
        double[] Normalize(double[] data, double mean, double std);

        double Normalize(double data, double mean, double std);

        double[] CalculateMovingAverage(double[] data, int period);

        double[] CalculateMovingStdDev(double[] data, int period);

        double[] CalculateMovingMedian(double[] data, int period);

        double[] CalculateMovingPercentile(double[] data, int period, double percentile);

        Tuple<double[], double[]> CalculateMovingLinearFit(double[] data, int period);

        double[] CalculateCCI(IEnumerable<Quote> quotes, int period);

        Tuple<double[], double[]> CalculateMacD(double[] close, int periodFast, int periodSlow, int signal);

        double[] CalculateRSI(double[] data, int period);

        double[] CalculateWR(IEnumerable<Quote> quotes, int period);

        double[] CalculateATR(IEnumerable<Quote> quotes, int period);

        double[] CalculateEMA(IEnumerable<Quote> quotes, int period);

        double[] CalculateHV(double[] data, int period);

        double[] CalculateLV(double[] data, int period);

        double[] CalculateStochK(double[] high, double[] low, double[] close, int fastKPeriod, int fastDPeriod);
    }
}