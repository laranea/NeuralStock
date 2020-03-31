namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using twentySix.NeuralStock.Core.Enums;
    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    public class ProfitLossCalculator
    {
        private readonly IStatisticsService _statisticsService;

        public ProfitLossCalculator(StockPortfolio portfolio, TrainingSession trainingSession, SortedList<DateTime, SignalEnum> signals)
        {
            this._statisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();

            this.Portfolio = portfolio;
            this.TrainingSession = trainingSession;
            this.Signals = signals;

            this.Calculate();
        }

        public StockPortfolio Portfolio { get; }

        public TrainingSession TrainingSession { get; }

        public SortedList<DateTime, SignalEnum> Signals { get; }

        public Dictionary<DateTime, double> SellSignals => this.Signals
            .Zip(
                this.TrainingSession.TestingHistoricalData.Quotes.Values,
                (s, q) => new KeyValuePair<Quote, SignalEnum>(q, s.Value)).Where(x => x.Value == SignalEnum.Sell)
            .ToDictionary(x => x.Key.Date, x => x.Key.High);

        public Dictionary<DateTime, double> BuySignals => this.Signals
            .Zip(
                this.TrainingSession.TestingHistoricalData.Quotes.Values,
                (s, q) => new KeyValuePair<Quote, SignalEnum>(q, s.Value)).Where(x => x.Value == SignalEnum.Buy)
            .ToDictionary(x => x.Key.Date, x => x.Key.Low);

        public SignalEnum LatestSignal => this.Signals.LastOrDefault().Value;

        public SignalEnum SecondLastSignal => this.Signals.ElementAtOrDefault(this.Signals.Count - 2).Value;

        public SignalEnum ThirdLastSignal => this.Signals.ElementAtOrDefault(this.Signals.Count - 3).Value;

        public Dictionary<DateTime, double> PortfolioTotalValue =>
            this.TrainingSession.TestingHistoricalData.Quotes.Values.ToDictionary(
                x => x.Date,
                x => this.Portfolio.GetValue(x.Date));

        public List<CompleteTransaction> CompleteTransactions { get; } = new List<CompleteTransaction>();

        public double PL => this.Portfolio.GetValue(this.TrainingSession.TestingHistoricalData.EndDate) - this.Portfolio.GetValue(this.TrainingSession.TestingHistoricalData.BeginDate);

        public double PLPercentage => this.PL / this.Portfolio.GetValue(this.TrainingSession.TestingHistoricalData.BeginDate);

        public double PLPercentageYear => this.PLPercentage
                                          * (365d / (this.TrainingSession.TestingHistoricalData.EndDate
                                                     - this.TrainingSession.TestingHistoricalData.BeginDate).TotalDays);

        public double BuyHold => (this.TrainingSession.TestingHistoricalData.Quotes.LastOrDefault().Value.Close
                                 - this.TrainingSession.TestingHistoricalData.Quotes.FirstOrDefault().Value.Close) / this.TrainingSession.TestingHistoricalData.Quotes.FirstOrDefault().Value.Close;

        public double BuyHoldDifference => this.BuyHold != 0 ? this.PLPercentage / this.BuyHold : 0d;

        public int NumberBuySignals => this.Signals.Count(x => x.Value == SignalEnum.Buy);

        public int NumberSellSignals => this.Signals.Count(x => x.Value == SignalEnum.Sell);

        public int NumberOfTrades => this.Portfolio.Trades.Count / 2;

        public int NumberOfCompleteTransactions => this.CompleteTransactions.Count;

        public double MaxPL => this.CompleteTransactions.Any() ? this.CompleteTransactions.Max(x => x.PL) : 0;

        public double MinPL => this.CompleteTransactions.Any() ? this.CompleteTransactions.Min(x => x.PL) : 0;

        public int NumberWinningTransactions => this.CompleteTransactions.Count(x => x.PL > 0);

        public double PercentageWinningTransactions => this.NumberWinningTransactions / (double)this.CompleteTransactions.Count;

        public int NumberLossingTransactions => this.CompleteTransactions.Count(x => x.PL < 0);

        public double MeanPL => this._statisticsService.Mean(this.CompleteTransactions.Select(x => x.PL).ToArray());

        public double StandardDeviationPL => this._statisticsService.StandardDeviation(this.CompleteTransactions.Select(x => x.PL).ToArray());

        public double MedianPL => this._statisticsService.Median(this.CompleteTransactions.Select(x => x.PL).ToArray());

        public double MedianWinningPL => this._statisticsService.Median(this.CompleteTransactions.Where(x => x.PL > 0).Select(x => x.PL).ToArray());

        public double MedianLossingPL => this._statisticsService.Median(this.CompleteTransactions.Where(x => x.PL < 0).Select(x => x.PL).ToArray());

        public Dictionary<string, int> CompleteTransactionsPLs => this._statisticsService.Bucketize(this.CompleteTransactions.Select(x => x.PL).ToArray(), 8);

        private void Calculate()
        {
            if (this.TrainingSession.TestingHistoricalData.Quotes.Count != this.Signals.Count)
            {
                return;
            }

            foreach (var quote in this.TrainingSession.TestingHistoricalData.Quotes)
            {
                var indexOfToday = this.TrainingSession.TestingHistoricalData.Quotes.IndexOfKey(quote.Key);
                var indexTomorrow = indexOfToday < this.TrainingSession.TestingHistoricalData.Quotes.Count - 2
                                        ? indexOfToday + 1
                                        : indexOfToday;
                var transactionPrice = this.TrainingSession.TestingHistoricalData.Quotes.ElementAt(indexTomorrow).Value.Open;

                if (this.Signals[quote.Key] == SignalEnum.Buy && this.Portfolio.GetMaxPurchaseVolume(this.TrainingSession.Stock, quote.Key, transactionPrice) > 1)
                {
                    var maxPurchaseVolume = this.Portfolio.GetMaxPurchaseVolume(this.TrainingSession.Stock, quote.Key, transactionPrice);
                    var trade = new Trade
                    {
                        Type = TransactionEnum.Buy,
                        Stock = this.TrainingSession.Stock,
                        Date = quote.Key,
                        NumberOfShares = maxPurchaseVolume,
                        Price = transactionPrice
                    };

                    this.Portfolio.Add(trade);
                }

                if (this.Signals[quote.Key] == SignalEnum.Sell && this.Portfolio.GetHoldings(quote.Key).ContainsKey(this.TrainingSession.Stock))
                {
                    var trade = new Trade
                    {
                        Type = TransactionEnum.Sell,
                        Stock = this.TrainingSession.Stock,
                        Date = quote.Key,
                        NumberOfShares = this.Portfolio.GetHoldings(quote.Key)[this.TrainingSession.Stock],
                        Price = transactionPrice
                    };

                    this.CompleteTransactions.Add(new CompleteTransaction(this.Portfolio.Trades.Last().Value, trade));
                    this.Portfolio.Add(trade);
                }
            }

            if (this.Portfolio.GetHoldings(this.TrainingSession.TestingHistoricalData.EndDate).Any())
            {
                var trade = new Trade
                {
                    Type = TransactionEnum.Sell,
                    Stock = this.TrainingSession.Stock,
                    Date = this.TrainingSession.TestingHistoricalData.EndDate,
                    NumberOfShares = this.Portfolio.GetHoldings(this.TrainingSession.TestingHistoricalData.EndDate)[this.TrainingSession.Stock],
                    Price = this.TrainingSession.TestingHistoricalData.Quotes[this.TrainingSession.TestingHistoricalData.EndDate].Close
                };

                this.CompleteTransactions.Add(new CompleteTransaction(this.Portfolio.Trades.Last().Value, trade));
                this.Portfolio.Add(trade);
            }
        }
    }
}