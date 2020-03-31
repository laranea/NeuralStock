namespace twentySix.NeuralStock.Core.Data.Sources
{
    using System.ComponentModel.Composition;

    using HtmlAgilityPack;

    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Models;

    [Export("GoogleFinanceDataSource", typeof(IDataSource))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class GoogleFinanceDataSource : IDataSource
    {
        public string Name => "Google Finance";

        public string GetSymbol(Stock stock)
        {
            if (stock.Country.Id == Singapore.CountryId)
            {
                return $"SGX:{stock.Symbol}";
            }

            if (stock.Country.Id == Portugal.CountryId)
            {
                return $"ELI:{stock.Symbol}";
            }

            return stock.Symbol;
        }

        public string GetName(string symbol)
        {
            var url = $"{Properties.Settings.Default.GoogleFinanceQuote}{symbol}/";
            var web = new HtmlWeb();
            var quotesDocument = web.Load(url);

            var name = "hello";

            return name;
        }
    }
}