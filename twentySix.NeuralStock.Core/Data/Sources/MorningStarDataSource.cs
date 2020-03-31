namespace twentySix.NeuralStock.Core.Data.Sources
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Net;

    using HtmlAgilityPack;

    using twentySix.NeuralStock.Core.Common;
    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Properties;

    [Export("MorningStarDataSource", typeof(IDataSource))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MorningStarDataSource : IDataSource
    {
        public string Name => "MorningStar";

        public string GetSymbol(Stock stock)
        {
            if (stock.Country.Id == Singapore.CountryId)
            {
                return $"XSES:{stock.Symbol}";
            }

            if (stock.Country.Id == Portugal.CountryId)
            {
                return $"XLIS:{stock.Symbol}";
            }

            return stock.Symbol;
        }

        public string GetName(Stock stock)
        {
            var url = Settings.Default.MorningStarQuote.Replace("{symbol}", this.GetSymbol(stock).Replace(":", "/"));

            try
            {
                using (var webclient = new WebClientExtended())
                {
                    webclient.Headers.Add(
                        HttpRequestHeader.Cookie,
                        Settings.Default.MorningStarCookie);

                    var data = webclient.DownloadString(url);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(data);
                    var result = doc.DocumentNode.SelectSingleNode("//span[@itemprop='name']");

                    return result?.InnerText ?? stock.Symbol;
                }
            }
            catch (Exception)
            {
                // ignored
                Debugger.Break();
            }

            return stock.Symbol;
        }
    }
}