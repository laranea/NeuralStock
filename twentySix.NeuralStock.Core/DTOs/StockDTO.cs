namespace twentySix.NeuralStock.Core.DTOs
{
    public class StockDTO
    {
        public int Id { get; set; }

        public string Symbol { get; set; }

        public string Name { get; set; }

        public int CountryId { get; set; }

        public int HistoricalDataId { get; set; }
    }
}