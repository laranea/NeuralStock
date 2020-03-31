namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DevExpress.Mvvm;

    using FANNCSharp;
    using FANNCSharp.Double;

    using twentySix.NeuralStock.Core.Common;
    using twentySix.NeuralStock.Core.DTOs;
    using twentySix.NeuralStock.Core.Enums;
    using twentySix.NeuralStock.Core.Extensions;
    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Messages;
    using twentySix.NeuralStock.Core.Services.Interfaces;
    using twentySix.NeuralStock.Core.Strategies;

    public class TrainingSession : BindableBase
    {
        private static readonly Random RandomGenerator = new Random();

        private static readonly object Locker = new object();

        private readonly IStatisticsService _statisticsService;

        private readonly SortedList<double, Prediction> _cachedPredictions = new SortedList<double, Prediction>(new DuplicateKeyComparer<double>());

        private volatile int _numberPredictionsComplete;

        private DateTime _startTime;

        public TrainingSession(StockPortfolio portfolio, Stock stock)
        {
            this._statisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();

            this.Portfolio = portfolio;
            this.Stock = stock;
        }

        public TrainingSession(StockPortfolio portfolio, Stock stock, BestNetworkDTO dto, NeuralStockSettings settings)
        {
            this._statisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();
            this.Portfolio = portfolio;
            this.Stock = stock;

            this.TrainSamplePercentage = settings.PercentageTraining;
            this.NumberAnns = settings.NumberANNs;
            this.NumberHiddenLayers = settings.NumberHiddenLayers;
            this.NumberNeuronsPerHiddenLayer = settings.NumberNeuronsHiddenLayer;

            this.BuyLevel = dto.BuyLevel;
            this.SellLevel = dto.SellLevel;

            var strategy = new StrategyI(StrategySettings.FromJson(dto.StrategySettings))
            {
                TrainingMeansInput = dto.TrainingMeansInput?.ToArray(),
                TrainingStdDevsInput = dto.TrainingStdDevsInput?.ToArray(),
                TrainingMeansOutput = dto.TrainingMeansOutput?.ToArray(),
                TrainingStdDevsOutput = dto.TrainingStdDevsOutput?.ToArray()
            };

            var tmpFileName = Path.GetTempFileName();
            File.WriteAllBytes(tmpFileName, dto.BestNeuralNet);
            var net = new NeuralNet(tmpFileName);

            this._cachedPredictions.Clear();
            this.SplitTrainTestData();

            var trainingTestingData = this.PrepareAnnData(strategy, false);

            var prediction = this.Predict(trainingTestingData, net, false);
            var profitLossCalculator = new ProfitLossCalculator(this.Portfolio.Reset(), this, prediction.Item1);
            this._cachedPredictions.Add(profitLossCalculator.PL, new Prediction(profitLossCalculator, strategy, net, prediction.Item2, prediction.Item3));
        }

        public int NumberAnns { get; set; } = 10;

        public double TrainSamplePercentage { get; set; } = 0.6;

        public int NumberHiddenLayers { get; set; } = 1;

        public int NumberNeuronsPerHiddenLayer { get; set; } = 10;

        public double BuyLevel
        {
            get => this.GetProperty(() => this.BuyLevel);
            set => this.SetProperty(() => this.BuyLevel, value);
        }

        public double SellLevel
        {
            get => this.GetProperty(() => this.SellLevel);
            set => this.SetProperty(() => this.SellLevel, value);
        }

        public StockPortfolio Portfolio { get; set; }

        public Stock Stock { get; }

        public HistoricalData TrainingHistoricalData
        {
            get => this.GetProperty(() => this.TrainingHistoricalData);
            set => this.SetProperty(() => this.TrainingHistoricalData, value);
        }

        public HistoricalData TestingHistoricalData
        {
            get => this.GetProperty(() => this.TestingHistoricalData);
            set => this.SetProperty(() => this.TestingHistoricalData, value);
        }

        public double Progress
        {
            get => this.GetProperty(() => this.Progress);
            set => this.SetProperty(() => this.Progress, value);
        }

        public TimeSpan TimeLeft
        {
            get => this.GetProperty(() => this.TimeLeft);
            set => this.SetProperty(() => this.TimeLeft, value);
        }

        public IEnumerable<Prediction> CachePredictions => this._cachedPredictions.Values;

        public ProfitLossCalculator BestProfitLossCalculator
        {
            get
            {
                lock (Locker)
                {
                    return this._cachedPredictions.LastOrDefault().Value?.ProfitLossCalculator;
                }
            }
        }

        public Prediction BestPrediction => this._cachedPredictions.LastOrDefault().Value;

        public Dictionary<string, int> AllNetworksPLs => this._statisticsService.Bucketize(this._cachedPredictions.Keys.ToArray(), 14);

        public List<Tuple<double, double>> AllNetworksPLsStdDevs
        {
            get
            {
                lock (Locker)
                {
                    return this._cachedPredictions.Values.ToList().Select(x => Tuple.Create(x.ProfitLossCalculator.PL, x.ProfitLossCalculator.StandardDeviationPL)).ToList();
                }
            }
        }

        public double AllNetworksPL => this._statisticsService.Median(this.AllNetworksPLsStdDevs.Select(x => x.Item1).ToArray());

        public double AllNetworksStdDev => this._statisticsService.StandardDeviation(this.AllNetworksPLsStdDevs.Select(x => x.Item1).ToArray());

        public double AllNetworksMin => this.AllNetworksPLsStdDevs.Any() ? this.AllNetworksPLsStdDevs.Select(x => x.Item1).Min() : 0;

        public double AllNetworksSigma => this.AllNetworksPL != 0 ? this.AllNetworksStdDev / this.AllNetworksPL : 0;

        public void FindBestAnn(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            Messenger.Default.Send(new TrainStatusMessage("Preparing data ..."));
            this._cachedPredictions.Clear();
            this.SplitTrainTestData();
            this._startTime = DateTime.Now;
            Messenger.Default.Send(new TrainStatusMessage("Training ..."));

            Parallel.For(
                0,
                this.NumberAnns,
                new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = 4 },
                i =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var strategy = new StrategyI(new StrategySettings());

                        var trainingTestingData = this.PrepareAnnData(strategy);

                        var net = this.Train(trainingTestingData);
                        var prediction = this.Predict(trainingTestingData, net);

                        var profitLossCalculator = new ProfitLossCalculator(this.Portfolio.Reset(), this, prediction.Item1);

                        _numberPredictionsComplete++;

                        lock (Locker)
                        {
                            this.AddBestNeuralNet(profitLossCalculator, strategy, net, prediction.Item2, prediction.Item3);

                            var currentPercentage = (this._numberPredictionsComplete + 1) / (double)this.NumberAnns;
                            this.Progress = currentPercentage * 100d;

                            var currentTimeTaken = DateTime.Now - this._startTime;

                            this.TimeLeft = currentTimeTaken - TimeSpan.FromSeconds(currentTimeTaken.TotalSeconds / currentPercentage);

                            this.RaisePropertyChanged(() => this.BestProfitLossCalculator);
                            this.RaisePropertyChanged(() => this.AllNetworksPLs);
                            this.RaisePropertyChanged(() => this.AllNetworksPLsStdDevs);
                            this.RaisePropertyChanged(() => this.AllNetworksPL);
                            this.RaisePropertyChanged(() => this.AllNetworksStdDev);
                            this.RaisePropertyChanged(() => this.AllNetworksMin);
                            this.RaisePropertyChanged(() => this.AllNetworksSigma);
                        }
                    });

            this.BuyLevel = this.BestPrediction.BuyLevel;
            this.SellLevel = this.BestPrediction.SellLevel;

            Messenger.Default.Send(new TrainStatusMessage("Done"));
        }

        public void SplitTrainTestData()
        {
            if (this.Stock?.HistoricalData == null)
            {
                throw new InvalidOperationException();
            }

            var splitData = SplitHistoricalData(this.Stock?.HistoricalData, this.TrainSamplePercentage);

            this.TrainingHistoricalData = splitData.Item1;
            this.TestingHistoricalData = splitData.Item2;
        }

        public BestNetworkDTO GetBestNetworkDTO()
        {
            var tmpFileName = Path.GetTempFileName();
            this.BestPrediction.Net.Save(tmpFileName);
            var neuralNetBytes = File.ReadAllBytes(tmpFileName);

            return new BestNetworkDTO
            {
                Id = this.Stock.GetUniqueId(),
                StockId = this.Stock.Id,
                StrategyId = this.BestPrediction.Strategy.Id,
                Timestamp = DateTime.Now,
                BuyLevel = this.BestPrediction.BuyLevel,
                SellLevel = this.BestPrediction.SellLevel,
                TrainingMeansInput = this.BestPrediction.Strategy.TrainingMeansInput?.ToArray(),
                TrainingStdDevsInput = this.BestPrediction.Strategy.TrainingStdDevsInput?.ToArray(),
                TrainingMeansOutput = this.BestPrediction.Strategy.TrainingMeansOutput?.ToArray(),
                TrainingStdDevsOutput = this.BestPrediction.Strategy.TrainingStdDevsOutput?.ToArray(),
                BestNeuralNet = neuralNetBytes,
                StrategySettings = this.BestPrediction.Strategy.Settings.GetJson()
            };
        }

        private static Tuple<HistoricalData, HistoricalData> SplitHistoricalData(HistoricalData data, double trainSamplePercentage)
        {
            if (data == null)
            {
                throw new InvalidOperationException();
            }

            var splitData = data / trainSamplePercentage;

            return Tuple.Create(splitData.Item1, splitData.Item2);
        }

        private Tuple<SortedList<DateTime, SignalEnum>, double, double> Predict(Tuple<List<AnnDataPoint>, List<AnnDataPoint>> trainingTestingData, NeuralNet net, bool resetLevels = true)
        {
            double buyLevel = this.BuyLevel;
            double sellLevel = this.SellLevel;

            if (resetLevels)
            {
                buyLevel = RandomGenerator.NextGaussian(0.8d, 0.05d);
                sellLevel = RandomGenerator.NextGaussian(-0.65d, 0.05d);
            }

            var result = new SortedList<DateTime, SignalEnum>();
            var testingData = trainingTestingData.Item2;

            foreach (var annDataPoint in testingData)
            {
                var predictedOutput = net.Run(annDataPoint.Inputs.ToArray())[0];

                var signal = SignalEnum.Neutral;

                if (predictedOutput >= buyLevel)
                {
                    signal = SignalEnum.Buy;
                }
                else if (predictedOutput <= sellLevel)
                {
                    signal = SignalEnum.Sell;
                }

                result.Add(annDataPoint.Date, signal);
            }

            return Tuple.Create(result, buyLevel, sellLevel);
        }

        private void AddBestNeuralNet(ProfitLossCalculator profitLossCalculator, StrategyI strategy, NeuralNet net, double buyLevel, double sellLevel)
        {
            this._cachedPredictions.Add(profitLossCalculator.PL, new Prediction(profitLossCalculator, strategy, net, buyLevel, sellLevel));
        }

        private Tuple<List<AnnDataPoint>, List<AnnDataPoint>> PrepareAnnData(StrategyI strategy, bool recalculateMeans = true)
        {
            if (this.TrainingHistoricalData == null || this.TestingHistoricalData == null)
            {
                throw new InvalidOperationException();
            }

            var trainingData = strategy.GetAnnData(this.TrainingHistoricalData, recalculateMeans);
            var testingData = strategy.GetAnnData(this.TestingHistoricalData, false);

            return Tuple.Create(trainingData, testingData);
        }

        private NeuralNet Train(Tuple<List<AnnDataPoint>, List<AnnDataPoint>> trainingTestingData)
        {
            var training = trainingTestingData.Item1;

            var trainData = new TrainingData();
            trainData.SetTrainData(
                training.Select(x => x.Inputs.ToArray()).ToArray(),
                training.Select(x => x.Outputs.ToArray()).ToArray());

            var layers = new List<uint> { (uint)training.First().Inputs.Length };
            Enumerable.Range(0, this.NumberHiddenLayers)
                .ToList()
                .ForEach(x => layers.Add((uint)this.NumberNeuronsPerHiddenLayer));
            layers.Add((uint)training.First().Outputs.Length);

            trainData.ShuffleTrainData();

            var net = new NeuralNet(NetworkType.LAYER, layers);

            net.ActivationFunctionHidden = ActivationFunction.ELLIOT;
            net.ActivationFunctionOutput = ActivationFunction.LINEAR;

            net.TrainOnData(trainData, 800, 0, 0.00001f);

            trainData.Dispose();

            return net;
        }
    }
}