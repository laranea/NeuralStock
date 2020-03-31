namespace twentySix.NeuralStock.Core.Services.Interfaces
{
    using System;
    using System.Threading.Tasks;

    using twentySix.NeuralStock.Core.Models;

    public interface IDownloaderService
    {
        Task<string> GetName(Stock stock);

        Task<HistoricalData> GetHistoricalData(Stock stock, DateTime startDate, DateTime? endDate = null, bool refresh = false);

        Task PopulateDividends(Stock stock, HistoricalData historicalData);
    }
}