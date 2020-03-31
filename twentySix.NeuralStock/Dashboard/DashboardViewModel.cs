namespace twentySix.NeuralStock.Dashboard
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;

    using JetBrains.Annotations;

    using twentySix.NeuralStock.Common;
    using twentySix.NeuralStock.Core.DTOs;
    using twentySix.NeuralStock.Core.Enums;
    using twentySix.NeuralStock.Core.Messages;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [POCOViewModel]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class DashboardViewModel : ComposedViewModelBase, IDataErrorInfo
    {
        private static readonly object Locker = new object();

        private CancellationTokenSource _cancellationTokenSource;

        private NeuralStockSettings _settings;

        protected DashboardViewModel()
        {
            Messenger.Default.Register<TrainStatusMessage>(this, this.OnTrainMessageStatus);

            // ReSharper disable once PossibleNullReferenceException
            Application.Current.MainWindow.Closing += (sender, args) => this.SaveFavourites();

            Task.Run(this.LoadData);
        }

        public virtual bool IsBusy { get; set; }

        [UsedImplicitly]
        public virtual StockPortfolio Portfolio { get; set; }

        public virtual ObservableCollection<DashboardPrediction> Predictions { get; set; }

        public virtual string Status { get; set; }

        public virtual SeverityEnum StatusSeverity { get; set; }

        public string Error => null;

        protected virtual INavigationService NavigationService => null;

        [Import]
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        protected IPersistenceService PersistenceService { get; set; }

        [Import]
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        protected IDownloaderService DownloaderService { get; set; }

        // ReSharper disable once StyleCop.SA1126
        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        [UsedImplicitly]
        public void NavigateTo(string view)
        {
            this.NavigationService.Navigate(view, null, true, this, true);
        }

        [UsedImplicitly]
        public void NavigateToTrainView(DashboardPrediction prediction)
        {
            this.NavigationService.Navigate("TrainView", null, prediction, this, true);
        }

        [UsedImplicitly]
        public async void Refresh()
        {
            await this.LoadData().ConfigureAwait(false);
            CommandManager.InvalidateRequerySuggested();
        }

        [UsedImplicitly]
        public bool CanRefresh()
        {
            return !this.IsBusy;
        }

        [UsedImplicitly]
        public void Cancel()
        {
            this._cancellationTokenSource.Cancel();
        }

        [UsedImplicitly]
        public bool CanCancel()
        {
            return this.IsBusy && this._cancellationTokenSource != null && this._cancellationTokenSource.Token.CanBeCanceled;
        }

        [UsedImplicitly]
        public async void Delete(DashboardPrediction prediction)
        {
            if (await this.PersistenceService.DeleteStockWithId(prediction.StockId))
            {
                Messenger.Default.Send(new TrainStatusMessage($"Deleted {prediction.Name}"));
                this.Refresh();
            }
            else
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not delete {prediction.Name}"));
            }
        }

        [UsedImplicitly]
        public bool CanDelete(DashboardPrediction prediction)
        {
            return prediction != null;
        }

        protected override async void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();

            // save favourites
            await Task.Run(this.SaveFavourites);
        }

        private void SaveFavourites()
        {
            this.PersistenceService.DeleteFavourites().Wait();
            var favourites = this.Predictions.Where(x => x.Favourite)
                .Select(x => new FavouriteDTO { StockId = x.TrainingSession.Stock.GetUniqueId() }).ToList();

            if (favourites.Any())
            {
                this.PersistenceService.SaveFavourites(favourites).Wait();
            }
        }

        private async Task LoadData()
        {
            try
            {
                if (this._cancellationTokenSource != null && this._cancellationTokenSource.Token.CanBeCanceled && !this._cancellationTokenSource.IsCancellationRequested)
                {
                    this._cancellationTokenSource.Cancel();
                    return;
                }

                this._cancellationTokenSource = new CancellationTokenSource();

                this.IsBusy = true;

                await this.LoadSettings().ConfigureAwait(false);
                this.SetPortfolio();

                // get list of all saved predictions
                var listBestPredictionsDtos = await this.PersistenceService.GetBestNetworkDTOs().ConfigureAwait(false);

                this.Predictions = new ObservableCollection<DashboardPrediction>();

                // for each
                foreach (var dto in listBestPredictionsDtos)
                {
                    if (this._cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    var stock = await this.PersistenceService.GetStockFromId(dto.StockId);
                    stock.HistoricalData = await this.DownloaderService.GetHistoricalData(stock, this._settings.StartDate, DateTime.Now, true).ConfigureAwait(false);

                    var dashboardPrediction = new DashboardPrediction { Symbol = stock.Symbol, Name = stock.Name, StockId = stock.GetUniqueId() };

                    lock (Locker)
                    {
                        Application.Current.Dispatcher?.Invoke(() => this.Predictions.Add(dashboardPrediction));
                    }

                    TrainingSession trainingSession;
                    try
                    {
                        trainingSession = await Task.Run(() => new TrainingSession(this.Portfolio, stock, dto, this._settings)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Messenger.Default.Send(new TrainStatusMessage($"Could setup training session for stock {stock.Name}: {ex.Message}", SeverityEnum.Error));
                        trainingSession = new TrainingSession(this.Portfolio, stock);
                    }

                    dashboardPrediction.TrainingSession = trainingSession;
                    dashboardPrediction.Close = trainingSession.Stock.HistoricalData?.Quotes?.LastOrDefault().Value?.Close ?? 0d;
                    dashboardPrediction.LastTrainingDate = dto.Timestamp;

                    if (trainingSession.Stock.HistoricalData != null)
                    {
                        dashboardPrediction.LastUpdate = trainingSession.Stock.HistoricalData.EndDate;
                    }
                }
            }
            finally
            {
                this.IsBusy = false;
                this._cancellationTokenSource = null;
            }
        }

        private async Task LoadSettings()
        {
            this._settings = await this.PersistenceService.GetSettings().ConfigureAwait(false) ?? new NeuralStockSettings();
        }

        private void SetPortfolio()
        {
            this.Portfolio = new StockPortfolio(this._settings.StartDate, this._settings.InitialCash);
        }

        private void OnTrainMessageStatus(TrainStatusMessage obj)
        {
            this.Status = obj.Message;
            this.StatusSeverity = obj.Severity;
        }
    }
}