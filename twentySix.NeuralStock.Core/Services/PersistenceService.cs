namespace twentySix.NeuralStock.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using LiteDB;

    using twentySix.NeuralStock.Core.DTOs;
    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [Export(typeof(IPersistenceService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PersistenceService : IPersistenceService, IDisposable
    {
        private const string DbCollectionStringStocks = "Stocks";
        private const string DbCollectionStringHistoricalData = "HistoricalData";
        private const string DbCollectionStringSettings = "Settings";
        private const string DbCollectionStringBestNetwork = "BestNetwork";
        private const string DbCollectionStringFavourites = "Favourites";

        private static readonly string DbLocation = Path.Combine(ApplicationHelper.GetAppDataFolder(), "DB.db");

        private static readonly string DbConnectionString = $"filename={DbLocation}; journal=false";

        private readonly LiteDatabase _db = new LiteDatabase(DbConnectionString);

        private readonly ILoggingService _loggingService;

        [ImportingConstructor]
        public PersistenceService(ILoggingService loggingService)
        {
            this._loggingService = loggingService;
        }

        public Task<List<Stock>> GetAllStocks()
        {
            return Task.Run(() =>
                {
                    var listStocks = new List<Stock>();

                    if (_db.CollectionExists(DbCollectionStringStocks) && _db.CollectionExists(DbCollectionStringHistoricalData))
                    {
                        try
                        {
                            var stocksDtos = _db.GetCollection<StockDTO>(DbCollectionStringStocks).FindAll();
                            var historicalDtos = _db.GetCollection<HistoricalDataDTO>(DbCollectionStringHistoricalData).FindAll();

                            listStocks.AddRange(stocksDtos.Select(stockDto => Stock.FromDTO(stockDto, historicalDtos.FirstOrDefault(x => x.Id == stockDto.HistoricalDataId))));
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(GetAllStocks)}: {ex}");
                        }
                    }

                    return listStocks;
                });
        }

        public Task<Stock> GetStockFromId(int id)
        {
            return Task.Run(() =>
                {
                    if (_db.CollectionExists(DbCollectionStringStocks) && _db.CollectionExists(DbCollectionStringHistoricalData))
                    {
                        try
                        {
                            var stockDto = _db.GetCollection<StockDTO>(DbCollectionStringStocks).FindById(id);
                            var historicalDto = _db.GetCollection<HistoricalDataDTO>(DbCollectionStringHistoricalData).FindById(stockDto.HistoricalDataId);

                            return Stock.FromDTO(stockDto, historicalDto);
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(GetStockFromId)}: {ex}");
                        }
                    }

                    return null;
                });
        }

        public Task<bool> SaveStock(Stock stock)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var stockDto = stock.GetDTO();
                            var historicalDataDto = stock.HistoricalData.GetDTO();

                            var stockTable = _db.GetCollection<StockDTO>(DbCollectionStringStocks);
                            var historicalTable = _db.GetCollection<HistoricalDataDTO>(DbCollectionStringHistoricalData);

                            historicalTable.Upsert(historicalDataDto);
                            stockDto.HistoricalDataId = historicalDataDto.Id;
                            stock.HistoricalData.Id = historicalDataDto.Id;
                            stockTable.Upsert(stockDto);
                            stock.Id = stockDto.Id;

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(SaveStock)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<bool> DeleteStock(Stock stock)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var stockDto = stock.GetDTO();
                            var historicalDto = stock.HistoricalData.GetDTO();

                            var stockTable = _db.GetCollection<StockDTO>(DbCollectionStringStocks);
                            var historicalTable = _db.GetCollection<HistoricalDataDTO>(DbCollectionStringHistoricalData);

                            historicalTable.Delete(historicalDto.Id);
                            stockTable.Delete(stockDto.Id);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(this.DeleteStock)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<bool> DeleteStockWithId(int id)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var stockTable = _db.GetCollection<StockDTO>(DbCollectionStringStocks);
                            var historicalTable = _db.GetCollection<HistoricalDataDTO>(DbCollectionStringHistoricalData);
                            var bestNetworksTable = _db.GetCollection<BestNetworkDTO>(DbCollectionStringBestNetwork);

                            var stockDto = stockTable.FindOne(x => x.Id == id);
                            var historicalDto = historicalTable.FindOne(x => x.Id == stockDto.HistoricalDataId);
                            var bestNetworkDto = bestNetworksTable.FindOne(x => x.StockId == stockDto.Id);

                            stockTable.Delete(stockDto.Id);
                            historicalTable.Delete(historicalDto.Id);
                            bestNetworksTable.Delete(bestNetworkDto.Id);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            this._loggingService.Warn($"{nameof(this.DeleteStockWithId)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<NeuralStockSettings> GetSettings()
        {
            return Task.Run(
                () =>
                    {
                        if (_db.CollectionExists(DbCollectionStringSettings))
                        {
                            try
                            {
                                var settingsDto = _db.GetCollection<NeuralStockSettingsDTO>(DbCollectionStringSettings).FindAll();

                                return NeuralStockSettings.FromDTO(settingsDto.FirstOrDefault());
                            }
                            catch (Exception ex)
                            {
                                this._loggingService.Warn($"{nameof(GetSettings)}: {ex}");
                            }
                        }

                        return null;
                    });
        }

        public Task<bool> SaveSettings(NeuralStockSettings settings)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var settingsDTO = settings.GetDTO();

                            var settingsTable = _db.GetCollection<NeuralStockSettingsDTO>(DbCollectionStringSettings);
                            settingsTable.Upsert(settingsDTO);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(SaveSettings)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<List<BestNetworkDTO>> GetBestNetworkDTOs()
        {
            return Task.Run(() =>
                {
                    var bestNetworkDtos = new List<BestNetworkDTO>();

                    if (this._db.CollectionExists(DbCollectionStringBestNetwork))
                    {
                        try
                        {
                            var dtos = this._db.GetCollection<BestNetworkDTO>(DbCollectionStringBestNetwork).FindAll();

                            bestNetworkDtos.AddRange(dtos);
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(this.GetBestNetworkDTOs)}: {ex}");
                        }
                    }

                    return bestNetworkDtos;
                });
        }

        public Task<BestNetworkDTO> GetBestNetworkDTOFromId(int id)
        {
            return Task.Run(() =>
                {
                    BestNetworkDTO bestNetworkDto = null;

                    if (this._db.CollectionExists(DbCollectionStringBestNetwork))
                    {
                        try
                        {
                            bestNetworkDto = this._db.GetCollection<BestNetworkDTO>(DbCollectionStringBestNetwork).FindById(id);
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(this.GetBestNetworkDTOFromId)}: {ex}");
                        }
                    }

                    return bestNetworkDto;
                });
        }

        public Task<bool> DeleteBestNetworkDTO(BestNetworkDTO bestNetwork)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var table = this._db.GetCollection<BestNetworkDTO>(DbCollectionStringBestNetwork);

                            table.Delete(x => x.StockId == bestNetwork.StockId);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            this._loggingService.Warn($"{nameof(this.DeleteBestNetworkDTO)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<bool> SaveBestNetwork(TrainingSession trainingSession)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var dto = trainingSession.GetBestNetworkDTO();

                            var table = this._db.GetCollection<BestNetworkDTO>(DbCollectionStringBestNetwork);
                            table.Upsert(dto);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(this.SaveBestNetwork)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<bool> SaveFavourite(FavouriteDTO dto)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var table = this._db.GetCollection<FavouriteDTO>(DbCollectionStringFavourites);
                            table.Upsert(dto);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(this.SaveFavourite)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<bool> SaveFavourites(IEnumerable<FavouriteDTO> dtos)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var table = this._db.GetCollection<FavouriteDTO>(DbCollectionStringFavourites);
                            table.Upsert(dtos);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(this.SaveFavourites)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<bool> DeleteFavourite(FavouriteDTO dto)
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            var table = this._db.GetCollection<FavouriteDTO>(DbCollectionStringFavourites);
                            table.Delete(x => x.StockId == dto.StockId);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            this._loggingService.Warn($"{nameof(this.DeleteFavourite)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<bool> DeleteFavourites()
        {
            return Task.Run(
                () =>
                    {
                        try
                        {
                            this._db.DropCollection(DbCollectionStringFavourites);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            this._loggingService.Warn($"{nameof(this.DeleteFavourite)}: {ex}");
                            return false;
                        }
                    });
        }

        public Task<List<FavouriteDTO>> GetFavourites()
        {
            return Task.Run(() =>
                {
                    var favouriteDtos = new List<FavouriteDTO>();

                    if (this._db.CollectionExists(DbCollectionStringFavourites))
                    {
                        try
                        {
                            var dtos = this._db.GetCollection<FavouriteDTO>(DbCollectionStringFavourites).FindAll();
                            favouriteDtos.AddRange(dtos);
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warn($"{nameof(this.GetFavourites)}: {ex}");
                        }
                    }

                    return favouriteDtos;
                });
        }

        public void Dispose()
        {
            this._db?.Dispose();
        }
    }
}