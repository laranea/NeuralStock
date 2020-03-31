namespace twentySix.NeuralStock.Core.Data.Sources
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Models;

    using YahooFinanceAPI;
    using YahooFinanceAPI.Models;

    [Export("YahooFinanceDataSource", typeof(IDataSource))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class YahooFinanceDataSource : IDataSource
    {
        public string Name => "Yahoo Finance";

        public string GetSymbol(Stock stock)
        {
            if (stock.Country.Id == Singapore.CountryId)
            {
                return $"{stock.Symbol}.SI";
            }

            if (stock.Country.Id == Portugal.CountryId)
            {
                return $"{stock.Symbol}.LS";
            }

            return stock.Symbol;
        }

        public HistoricalData GetHistoricalData(Stock stock, DateTime startDate, DateTime endDate)
        {

            var rawHistoricalData = new List<HistoryPrice>();

            var numberOfTries = 0;
            while (rawHistoricalData.Count == 0 && numberOfTries < 4)
            {
                rawHistoricalData = Historical.GetPriceAsync(this.GetSymbol(stock), startDate, endDate).Result;
                numberOfTries++;
            }

            var historicalData = rawHistoricalData.GroupBy(x => x.Date).Select(grp => grp.First());

            var quotes = new SortedList<DateTime, Quote>(
                historicalData.ToDictionary(
                    x => x.Date,
                    x => new Quote
                    {
                        Date = x.Date,
                        Open = x.Open,
                        Close = x.Close,
                        AdjClose = x.AdjClose,
                        Low = x.Low,
                        High = x.High,
                        Volume = x.Volume
                    }));

            return new HistoricalData(quotes) { Id = stock.GetUniqueId() };
        }

        public List<Dividend> GetDividendsData(Stock stock, DateTime startDate, DateTime endDate)
        {
            return Historical.GetDividendAsync(this.GetSymbol(stock), startDate, endDate).Result;
        }
    }
}