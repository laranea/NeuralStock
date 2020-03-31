namespace twentySix.NeuralStock.Core.Services
{
    using System;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading.Tasks;

    using twentySix.NeuralStock.Core.Data.Sources;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [Export(typeof(IDownloaderService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DownloaderService : IDownloaderService, IDisposable
    {
        private readonly ILoggingService _loggingService;

        private readonly YahooFinanceDataSource _yahooFinanceDataSource;

        private readonly MorningStarDataSource _morningStarDataSource;

        [ImportingConstructor]
        public DownloaderService(
            ILoggingService loggingService,
            [Import("YahooFinanceDataSource")]IDataSource yahooDataSource,
            [Import("GoogleFinanceDataSource")]IDataSource googleDataSource,
            [Import("MorningStarDataSource")]IDataSource morningStarDataSource)
        {
            this._loggingService = loggingService;
            this._yahooFinanceDataSource = yahooDataSource as YahooFinanceDataSource;
            this._morningStarDataSource = morningStarDataSource as MorningStarDataSource;
        }

        public void Dispose()
        {
        }

        public async Task<string> GetName(Stock stock)
        {
            try
            {
                return await Task.Run(() => this._morningStarDataSource.GetName(stock));
            }
            catch (Exception ex)
            {
                this._loggingService?.Warn($"{nameof(this.GetName)}: {ex}");
                return string.Empty;
            }
        }

        public async Task<HistoricalData> GetHistoricalData(Stock stock, DateTime startDate, DateTime? endDate = null, bool refresh = false)
        {
            try
            {
                if (refresh || stock.HistoricalData == null || !stock.HistoricalData.Quotes.Any())
                {
                    var historicalData = await Task.Run(() => this._yahooFinanceDataSource.GetHistoricalData(stock, startDate, endDate ?? DateTime.Now));
                    await this.PopulateDividends(stock, historicalData);
                    return historicalData;
                }

                HistoricalData preHistoricalData = null;
                if (startDate < stock.HistoricalData.BeginDate)
                {
                    preHistoricalData = await Task.Run(() => this._yahooFinanceDataSource.GetHistoricalData(stock, startDate, stock.HistoricalData.BeginDate));
                }

                // always download latest quote
                HistoricalData postHistoricalData = null;
                if (endDate == null || endDate >= stock.HistoricalData.EndDate)
                {
                    postHistoricalData = await Task.Run(() => this._yahooFinanceDataSource.GetHistoricalData(stock, stock.HistoricalData.EndDate, endDate ?? DateTime.Now));
                }

                var currentHistoricalData = stock.HistoricalData;

                var result = currentHistoricalData + preHistoricalData + postHistoricalData;
                await this.PopulateDividends(stock, result);
                return result;
            }
            catch (Exception ex)
            {
                _loggingService?.Warn($"{nameof(this.GetName)}: {ex}");
                return null;
            }
        }

        public async Task PopulateDividends(Stock stock, HistoricalData historicalData)
        {
            var dividendsHistory = await Task.Run(() => this._yahooFinanceDataSource.GetDividendsData(stock, historicalData.BeginDate, historicalData.EndDate));
            dividendsHistory.ForEach(dividend =>
                {
                    historicalData.Quotes.FirstOrDefault(x => x.Key >= dividend.Date).Value.Dividend = dividend.Div;
                });
        }
    }
}