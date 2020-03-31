namespace twentySix.NeuralStock.Train
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;

    using JetBrains.Annotations;

    using twentySix.NeuralStock.Common;
    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Enums;
    using twentySix.NeuralStock.Core.Messages;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;
    using twentySix.NeuralStock.Dashboard;

    [POCOViewModel]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class TrainViewModel : ComposedViewModelBase, IDataErrorInfo
    {
        private CancellationTokenSource _cancellationTokenSource;

        private NeuralStockSettings _settings;

        protected TrainViewModel()
        {
            Messenger.Default.Register<TrainStatusMessage>(this, this.OnTrainMessageStatus);
        }

        public virtual bool IsBusy { get; set; }

        public virtual bool IsTraining { get; set; }

        public virtual Stock Stock { get; set; }

        public virtual string StockSymbol { get; set; }

        [UsedImplicitly]
        public virtual StockPortfolio Portfolio { get; set; }

        public virtual TrainingSession TrainingSession { get; set; }

        [UsedImplicitly]
        public virtual HistoricalData TrainingData { get; set; }

        public virtual HistoricalData TestingData { get; set; }

        public virtual string Status { get; set; }

        public virtual SeverityEnum StatusSeverity { get; set; }

        public virtual bool IsCountrySingapore { get; set; } = true;

        public virtual Quote LastQuote => this.Stock?.HistoricalData?.Quotes.LastOrDefault().Value;

        public string Error => null;

        protected virtual INavigationService NavigationService => null;

        [UsedImplicitly]
        [Import]
        protected IPersistenceService PersistenceService { get; set; }

        [UsedImplicitly]
        [Import]
        protected IDownloaderService DownloaderService { get; set; }

        // ReSharper disable once StyleCop.SA1126
        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        [UsedImplicitly]
        public void NavigateTo(string view)
        {
            this.NavigationService.Navigate(view, null, this);
        }

        [UsedImplicitly]
        public async Task DownloadData()
        {
            this.IsBusy = true;

            try
            {
                this.ClearStatus();

                this.Stock = new Stock
                {
                    Symbol = this.StockSymbol,
                    Country = this.IsCountrySingapore ? new Singapore() as ICountry : new Portugal()
                };
                this.Stock.Id = this.Stock.GetUniqueId();

                this.RaisePropertyChanged(() => this.LastQuote);

                Messenger.Default.Send(new TrainStatusMessage("Loading settings"));

                await this.LoadSettings().ConfigureAwait(false);

                Messenger.Default.Send(new TrainStatusMessage($"Downloading data for stock {this.Stock.Symbol}"));

                var name = await this.DownloaderService.GetName(this.Stock).ConfigureAwait(false);

                if (name.Equals(this.Stock.Symbol))
                {
                    Messenger.Default.Send(new TrainStatusMessage($"Could not download data for stock {this.Stock.Symbol}", SeverityEnum.Error));
                    return;
                }

                this.Stock.Name = name;
                this.Stock.HistoricalData = await this.DownloaderService.GetHistoricalData(this.Stock, this._settings.StartDate, DateTime.Now)
                                                .ConfigureAwait(false);

                this.ResetTrainingSession();
                this.SplitHistoricalData();

                this.RaisePropertyChanged(() => this.LastQuote);
                CommandManager.InvalidateRequerySuggested();

                await this.PersistenceService.SaveStock(this.Stock).ConfigureAwait(false);

                Messenger.Default.Send(new TrainStatusMessage($"Finished downloading data for stock {this.Stock.Symbol}"));
            }
            catch (Exception ex)
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not download data for stock {this.Stock.Symbol}: {ex.Message}", SeverityEnum.Error));
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        [UsedImplicitly]
        public bool CanDownloadData()
        {
            return !string.IsNullOrEmpty(this.StockSymbol);
        }

        [UsedImplicitly]
        public async void Train()
        {
            this.IsBusy = true;
            this.IsTraining = true;

            if (!this.Stock.HistoricalData.Quotes.Any())
            {
                Messenger.Default.Send(new TrainStatusMessage($"Historical data for stock {this.Stock.Name} not downloaded.", SeverityEnum.Error));
                return;
            }

            try
            {
                if (this._cancellationTokenSource != null && this._cancellationTokenSource.Token.CanBeCanceled && !this._cancellationTokenSource.IsCancellationRequested)
                {
                    this._cancellationTokenSource.Cancel();
                    return;
                }

                await this.DownloadData().ConfigureAwait(false);

                this._cancellationTokenSource = new CancellationTokenSource();
                await Task.Run(() => this.TrainingSession.FindBestAnn(this._cancellationTokenSource.Token)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not train {this.Stock.Name}. Exception: {ex.Message}", SeverityEnum.Error));
            }
            finally
            {
                this.IsTraining = false;
                this.IsBusy = false;
                this._cancellationTokenSource = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        [UsedImplicitly]
        public bool CanTrain()
        {
            return !string.IsNullOrEmpty(this.StockSymbol) && (this.Stock?.HistoricalData?.Quotes.Any() ?? false);
        }

        [UsedImplicitly]
        public async void Save()
        {
            try
            {
                this.IsBusy = true;
                if (!await this.PersistenceService.SaveBestNetwork(this.TrainingSession).ConfigureAwait(false))
                {
                    throw new Exception("Error found while saving the best network");
                }

                Messenger.Default.Send(new TrainStatusMessage($"Saved best network for {this.Stock.Symbol}"));
            }
            catch (Exception ex)
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not save {this.Stock.Symbol}: {ex.Message}", SeverityEnum.Error));
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        [UsedImplicitly]
        public bool CanSave()
        {
            return !this.IsBusy && !this.IsTraining && this.TrainingSession?.BestPrediction != null;
        }

        protected override void OnNavigatedTo()
        {
            if (this.Parameter is DashboardPrediction dashboardPrediction)
            {
                this.Stock = dashboardPrediction.TrainingSession.Stock;
                this.StockSymbol = this.Stock.Symbol;
                this.IsCountrySingapore = this.Stock.Country is Singapore;
                this.TrainingSession = dashboardPrediction.TrainingSession;
                this.TrainingData = this.TrainingSession.TrainingHistoricalData;
                this.TestingData = this.TrainingSession.TestingHistoricalData;
                this.RaisePropertyChanged(() => this.LastQuote);
            }

            if (this.Parameter is bool clearData)
            {
                // ReSharper disable once StyleCop.SA1126
                if (clearData)
                {
                    this.ClearStatus();
                    this.StockSymbol = string.Empty;
                    this.Stock = null;
                    this.TrainingSession = null;
                    this.RaisePropertyChanged(() => this.LastQuote);
                }
            }
        }

        private void ClearStatus()
        {
            this.TrainingData = null;
            this.TestingData = null;

            Messenger.Default.Send(new TrainStatusMessage(string.Empty));
        }

        private void ResetTrainingSession()
        {
            this.Portfolio = new StockPortfolio(this._settings.StartDate, this._settings.InitialCash);

            this.TrainingSession = new TrainingSession(this.Portfolio, this.Stock)
            {
                TrainSamplePercentage = this._settings.PercentageTraining,
                NumberAnns = this._settings.NumberANNs,
                NumberHiddenLayers = this._settings.NumberHiddenLayers,
                NumberNeuronsPerHiddenLayer = this._settings.NumberNeuronsHiddenLayer
            };
        }

        private void SplitHistoricalData()
        {
            this.TrainingSession.SplitTrainTestData();
            this.TrainingData = this.TrainingSession.TrainingHistoricalData;
            this.TestingData = this.TrainingSession.TestingHistoricalData;
        }

        private async Task LoadSettings()
        {
            this._settings = await this.PersistenceService.GetSettings().ConfigureAwait(false) ?? new NeuralStockSettings();
        }

        private void OnTrainMessageStatus(TrainStatusMessage obj)
        {
            this.Status = obj.Message;
            this.StatusSeverity = obj.Severity;
        }
    }
}