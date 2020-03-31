namespace twentySix.NeuralStock.Core.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    public class StrategyI : StrategyBase
    {
        private static readonly object Locker = new object();

        public StrategyI(StrategySettings settings)
        {
            this.Settings = settings ?? new StrategySettings();

            lock (Locker)
            {
                this.StatisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();
                this.DataProcessorService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IDataProcessorService>();
            }
        }

        public override int Id => 1;

        public override StrategySettings Settings { get; protected set; }

        protected override IList<AnnDataPoint> GetRawAnnDataPoints(HistoricalData historicalData)
        {
            var result = new List<AnnDataPoint>();
            var historicalQuotes = historicalData.Quotes.Values;

            var close = historicalQuotes.Select(x => x.Close).ToArray();
            var volume = historicalQuotes.Select(x => x.Volume).ToArray();

            var movingAverageCloseFast = this.DataProcessorService.CalculateMovingAverage(close, this.Settings.MovingAverageCloseFast);
            var movingAverageCloseSlow = this.DataProcessorService.CalculateMovingAverage(close, this.Settings.MovingAverageCloseSlow);
            var cci = this.DataProcessorService.CalculateCCI(historicalQuotes, this.Settings.CCI);
            var rsi = this.DataProcessorService.CalculateRSI(close, this.Settings.RSI);
            var rsi2 = this.DataProcessorService.CalculateRSI(close, this.Settings.RSI2);
            var macD = this.DataProcessorService.CalculateMacD(close, this.Settings.MacdFast, this.Settings.MacdSlow, this.Settings.MacdSignal);
            var hv = this.DataProcessorService.CalculateHV(close, this.Settings.Hv1);
            var fitClose = this.DataProcessorService.CalculateMovingLinearFit(close, this.Settings.FitClose);
            var fitOfFit = this.DataProcessorService.CalculateMovingLinearFit(fitClose.Item2, this.Settings.FitOfFit);
            var fitRSI = this.DataProcessorService.CalculateMovingLinearFit(this.DataProcessorService.CalculateRSI(close, this.Settings.RSI1Fit), this.Settings.RSI2Fit);
            
            int fwdDays = this.Settings.FwdDays;
            int yesterdayStep = 1;

            for (int i = 0; i < historicalQuotes.Count; i++)
            {
                var fwdDate = i + fwdDays >= historicalQuotes.Count
                                  ? historicalQuotes.Count - 1
                                  : i + fwdDays;

                var yesterdayIndex = i - yesterdayStep >= 0 ? i - yesterdayStep : 0;
                var yesterday = historicalQuotes[yesterdayIndex];
                var today = historicalQuotes[i];
                var future = historicalQuotes[fwdDate];

                var percentageChange = ((future.Close - today.Close) / today.Close) * 100d;

                var annDataPoint = new AnnDataPoint
                {
                    Date = today.Date,
                    Inputs = new[]
                                 {
                                     Math.Sinh(rsi[i]) + Math.Sinh(rsi2[i]),
                                     today.Close - today.High,
                                     yesterday.Close - yesterday.High,
                                     Math.Abs(volume[i] - volume[yesterdayIndex]),
                                     movingAverageCloseFast[i] / (movingAverageCloseSlow[yesterdayIndex] + 1E-6),
                                     movingAverageCloseSlow[i] / (movingAverageCloseFast[yesterdayIndex] + 1E-6),
                                     cci[i],
                                     today.Date.Month,
                                     macD.Item1[i],
                                     macD.Item2[i],
                                     rsi[i],
                                     hv[i],
                                     fitClose.Item2[i],
                                     fitOfFit.Item2[i],
                                     fitRSI.Item2[i],
                                     MathNet.Numerics.Distance.Pearson(
                                         new[]
                                             {
                                                 today.Close, today.High, today.Open, today.Low, today.Volume,
                                                 rsi[i], cci[i], macD.Item2[i]
                                             },
                                         new[]
                                             {
                                                 yesterday.Close, yesterday.High, yesterday.Open, yesterday.Low, yesterday.Volume,
                                                 rsi[yesterdayIndex], cci[yesterdayIndex], macD.Item2[yesterdayIndex]
                                             })
                                 },
                    Outputs = new[]
                                  {
                                      percentageChange > this.Settings.PercentageChangeHigh ? 1d : percentageChange < this.Settings.PercentageChangeLow ? -1d : 0d
                                  }
                };

                result.Add(annDataPoint);
            }

            return result;
        }
    }
}