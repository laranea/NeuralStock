namespace twentySix.NeuralStock.Core.Services.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using twentySix.NeuralStock.Core.DTOs;
    using twentySix.NeuralStock.Core.Models;

    public interface IPersistenceService
    {
        Task<List<Stock>> GetAllStocks();

        Task<Stock> GetStockFromId(int id);

        Task<bool> SaveStock(Stock stock);

        Task<bool> DeleteStock(Stock stock);

        Task<bool> DeleteStockWithId(int id);

        Task<NeuralStockSettings> GetSettings();

        Task<bool> SaveSettings(NeuralStockSettings settings);

        Task<List<BestNetworkDTO>> GetBestNetworkDTOs();

        Task<BestNetworkDTO> GetBestNetworkDTOFromId(int id);

        Task<bool> DeleteBestNetworkDTO(BestNetworkDTO bestNetwork);

        Task<bool> SaveBestNetwork(TrainingSession trainingSession);

        Task<bool> SaveFavourite(FavouriteDTO dto);

        Task<bool> SaveFavourites(IEnumerable<FavouriteDTO> dto);

        Task<bool> DeleteFavourite(FavouriteDTO dto);

        Task<bool> DeleteFavourites();

        Task<List<FavouriteDTO>> GetFavourites();
    }
}