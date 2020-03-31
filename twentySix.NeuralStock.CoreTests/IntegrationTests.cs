namespace twentySix.NeuralStock.CoreTests
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Data.Sources;
    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [TestFixture]
    public class IntegrationTests
    {
        private IDownloaderService _downloadService;
        private TrainingSession _trainingSession;
        private StockPortfolio _portfolio;
        private Stock _stock;
        private DateTime _startDate;

        [SetUp]
        public void SetUp()
        {
            this._startDate = new DateTime(2017, 2, 11);

            this._downloadService = new DownloaderService(
                null,
                new YahooFinanceDataSource(),
                new GoogleFinanceDataSource(),
                new MorningStarDataSource());

            this._stock = new Stock
            {
                Country = new Singapore(),
                Symbol = "S63"
            };

            this._portfolio = new StockPortfolio(this._startDate, 50000);

            var callingAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var aggCatalog = new AggregateCatalog();
            aggCatalog.Catalogs.Add(new DirectoryCatalog(callingAssemblyLocation ?? throw new InvalidOperationException(), "twentySix.*.dll"));
            var compositionContainer = new CompositionContainer(aggCatalog);
            ApplicationHelper.StartUp(new LoggingService(), compositionContainer);
        }

        [TearDown]
        public void TearDown()
        {
            this._downloadService = null;
            this._stock = null;
        }

        [Test]

        [TestCase(2, 9)]
        public async Task FullTest(int numberOfHiddenLayers, int numberOfNeurons)
        {
            this._stock.Name = await this._downloadService.GetName(this._stock);

            this._stock.HistoricalData = await this._downloadService.GetHistoricalData(this._stock, this._startDate, refresh: true);

            this._trainingSession =
                new TrainingSession(this._portfolio, this._stock)
                {
                    NumberAnns = 1500,
                    NumberHiddenLayers = numberOfHiddenLayers,
                    NumberNeuronsPerHiddenLayer = numberOfNeurons
                };

            var timer = new Stopwatch();
            timer.Start();

            this._trainingSession.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == "BestProfitLossCalculator" && timer.ElapsedMilliseconds > 15000)
                    {
                        Trace.Write($"\n{this._trainingSession.AllNetworksPLsStdDevs.Count:N0}");
                        Trace.Write($" -> PL: {this._trainingSession.BestProfitLossCalculator.PL:C2}");
                        Trace.Write($" ({this._trainingSession.BestProfitLossCalculator.PLPercentage:P2})");
                        Trace.Write($" | median: {this._trainingSession.AllNetworksPL:C2}");
                        Trace.Write($" | acc: {this._trainingSession.BestProfitLossCalculator.PercentageWinningTransactions:P2}");

                        timer.Restart();
                    }
                };

            this._trainingSession.FindBestAnn(new CancellationToken());

            Console.Write("PL: {0:C2}", this._trainingSession.BestProfitLossCalculator.PL);
            Console.Write(" ({0:P2})", this._trainingSession.BestProfitLossCalculator.PLPercentage);
            Console.Write(" | median: {0:C2}", this._trainingSession.AllNetworksPL);
            Console.Write(" | acc: {0:P2}", this._trainingSession.BestProfitLossCalculator.PercentageWinningTransactions);

            Assert.Pass();
        }
    }
}