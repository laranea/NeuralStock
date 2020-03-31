// ReSharper disable StyleCop.SA1407
namespace twentySix.NeuralStock.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    using MathNet.Numerics;

    using NetTrader.Indicator;

    using TicTacTec.TA.Library;

    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [Export(typeof(IDataProcessorService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DataProcessorService : IDataProcessorService
    {
        private readonly IStatisticsService _statisticsDataService;

        [ImportingConstructor]
        public DataProcessorService(IStatisticsService statisticsService)
        {
            this._statisticsDataService = statisticsService;
        }

        public double[] Normalize(double[] data, double mean, double std)
        {
            return data.Select(x => (x - mean) / std).ToArray();
        }

        public double Normalize(double data, double mean, double std)
        {
            return (data - mean) / std;
        }

        public double[] CalculateMovingAverage(double[] data, int period)
        {
            var reversed = data.Reverse().ToArray();
            double[] result = new double[reversed.Length];

            Core.MovingAverage(
                0,
                reversed.Length - 1,
                reversed,
                period,
                Core.MAType.Sma,
                out _,
                out _,
                result);
            return result.Reverse().ToArray();
        }

        public double[] CalculateMovingStdDev(double[] data, int period)
        {
            double[] result = new double[data.Length];

            for (int i = data.Length - 1; i >= 0; i--)
            {
                var numTake = i - period >= 0 ? period : i;
                var subset = data.Skip(i + 1 - numTake).Take(numTake).ToList();

                result[i] = this._statisticsDataService.StandardDeviation(subset.DefaultIfEmpty().ToArray());
            }

            return result;
        }

        public double[] CalculateMovingMedian(double[] data, int period)
        {
            double[] result = new double[data.Length];

            for (int i = data.Length - 1; i >= 0; i--)
            {
                var numTake = i - period >= 0 ? period : i;
                var subset = data.Skip(i + 1 - numTake).Take(numTake).ToList();

                result[i] = this._statisticsDataService.Median(subset.DefaultIfEmpty().ToArray());
            }

            return result;
        }

        public double[] CalculateMovingPercentile(double[] data, int period, double percentile)
        {
            double[] result = new double[data.Length];

            for (int i = data.Length - 1; i >= 0; i--)
            {
                var numTake = i - period >= 0 ? period : i;
                var subset = data.Skip(i + 1 - numTake).Take(numTake).ToList();

                result[i] = this._statisticsDataService.Percentile(subset.DefaultIfEmpty().ToArray(), percentile);
            }

            return result;
        }

        public Tuple<double[], double[]> CalculateMovingLinearFit(double[] data, int period)
        {
            double[] a = new double[data.Length];
            double[] b = new double[data.Length];

            for (int i = data.Length - 1; i >= 0; i--)
            {
                var numTake = i - period >= 0 ? period : i;
                var subset = data.Skip(i + 1 - numTake).Take(numTake).ToArray();

                Tuple<double, double> fit = Tuple.Create(0d, 0d);

                if (subset.Length >= 2)
                {
                    fit = Fit.Line(Enumerable.Range(0, subset.Length).Select(_ => (double)_).ToArray(), subset);
                }

                a[i] = fit.Item1;
                b[i] = fit.Item2;
            }

            return Tuple.Create(a, b);
        }

        public double[] CalculateCCI(IEnumerable<Quote> quotes, int period)
        {
            var listOhlc = quotes.Select(
                x => new Ohlc
                {
                    Date = x.Date,
                    Close = x.Close,
                    AdjClose = x.Close,
                    High = x.High,
                    Low = x.Low,
                    Open = x.Open,
                    Volume = x.Volume
                }).ToList();

            var cci = new CCI(period, 0.015);
            cci.Load(listOhlc);
            var result = cci.Calculate();

            return result.Values.Select(x => x.GetValueOrDefault()).ToArray();
        }

        public double[] CalculateWR(IEnumerable<Quote> quotes, int period)
        {
            var listOhlc = quotes.Select(
                x => new Ohlc
                {
                    Date = x.Date,
                    Close = x.Close,
                    AdjClose = x.Close,
                    High = x.High,
                    Low = x.Low,
                    Open = x.Open,
                    Volume = x.Volume
                }).ToList();

            var wr = new WPR(period);
            wr.Load(listOhlc);
            var result = wr.Calculate();

            return result.Values.Select(x => x.GetValueOrDefault()).ToArray();
        }

        public double[] CalculateATR(IEnumerable<Quote> quotes, int period)
        {
            var listOhlc = quotes.Select(
                x => new Ohlc
                {
                    Date = x.Date,
                    Close = x.Close,
                    AdjClose = x.Close,
                    High = x.High,
                    Low = x.Low,
                    Open = x.Open,
                    Volume = x.Volume
                }).ToList();

            var atr = new ATR(period);
            atr.Load(listOhlc);
            var result = atr.Calculate();

            return result.ATR.Select(x => x.GetValueOrDefault()).ToArray();
        }

        public double[] CalculateEMA(IEnumerable<Quote> quotes, int period)
        {
            var listOhlc = quotes.Select(
                x => new Ohlc
                {
                    Date = x.Date,
                    Close = x.Close,
                    AdjClose = x.Close,
                    High = x.High,
                    Low = x.Low,
                    Open = x.Open,
                    Volume = x.Volume
                }).ToList();

            var ema = new EMA(period, true);
            ema.Load(listOhlc);
            var result = ema.Calculate();

            return result.Values.Select(x => x.GetValueOrDefault()).ToArray();
        }

        public Tuple<double[], double[]> CalculateMacD(double[] close, int periodFast, int periodSlow, int signal)
        {
            var ma1 = CalculateMovingAverage(close, periodFast);
            var ma2 = CalculateMovingAverage(close, periodSlow);
            var average = CalculateMovingAverage(close, signal);

            var macd = Enumerable.Range(0, ma1.Length).Select(i => ma1[i] - ma2[i]).ToList();
            var histo = Enumerable.Range(0, ma1.Length).Select(i => macd[i] - average[i]);

            return Tuple.Create(macd.ToArray(), histo.ToArray());
        }

        public double[] CalculateRSI(double[] data, int period)
        {
            double[] result = new double[data.Length];

            var differences = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                differences[i] = i == 0 ? 0 : data[i] - data[i - 1];
            }

            for (int i = 0; i < data.Length; i++)
            {
                var numTake = period;
                if (i < numTake)
                {
                    result[i] = 0;
                    continue;
                }

                var subSet = differences.Skip(i + 1 - numTake).Take(numTake).ToArray();
                var averageGain = subSet.Select(x => Math.Max(0, x)).Average();
                var averageLoss = subSet.Select(x => Math.Max(0, -x)).Average();
                var rs = averageGain / (averageLoss + double.Epsilon);
                result[i] = 100d - 100d / (1d + rs);
            }

            return result;
        }

        public double[] CalculateHV(double[] data, int period)
        {
            double[] result = new double[data.Length];

            for (int i = data.Length - 1; i >= 0; i--)
            {
                var numTake = i - period >= 0 ? period : i;
                var subset = data.Skip(i + 1 - numTake).Take(numTake).ToList();

                result[i] = subset.DefaultIfEmpty().Max();
            }

            return result;
        }

        public double[] CalculateLV(double[] data, int period)
        {
            double[] result = new double[data.Length];

            for (int i = data.Length - 1; i >= 0; i--)
            {
                var numTake = i - period >= 0 ? period : i;
                var subset = data.Skip(i + 1 - numTake).Take(numTake).ToList();

                result[i] = subset.DefaultIfEmpty().Min();
            }

            return result;
        }

        public double[] CalculateStochK(double[] high, double[] low, double[] close, int fastKPeriod, int fastDPeriod)
        {
            double[] slowk = new double[high.Length];
            double[] slowd = new double[high.Length];

            var rhigh = high.Reverse().ToArray();
            var rlow = low.Reverse().ToArray();
            var rclose = close.Reverse().ToArray();

            Core.StochF(
                0,
                rhigh.Length - 1,
                rhigh,
                rlow,
                rclose,
                fastKPeriod,
                fastDPeriod,
                Core.MAType.Sma,
                out _,
                out _,
                slowk,
                slowd);
            return slowk;
        }
    }
}