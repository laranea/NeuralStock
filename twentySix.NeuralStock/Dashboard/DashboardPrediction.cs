namespace twentySix.NeuralStock.Dashboard
{
    using System;

    using DevExpress.Mvvm;

    using twentySix.NeuralStock.Core.Models;

    public class DashboardPrediction : BindableBase
    {
        public int StockId { get; set; }

        public bool Favourite
        {
            get => this.GetProperty(() => this.Favourite);
            set => this.SetProperty(() => this.Favourite, value);
        }

        public string Symbol
        {
            get => this.GetProperty(() => this.Symbol);
            set => this.SetProperty(() => this.Symbol, value);
        }

        public string Name
        {
            get => this.GetProperty(() => this.Name);
            set => this.SetProperty(() => this.Name, value);
        }

        public DateTime LastUpdate
        {
            get => this.GetProperty(() => this.LastUpdate);
            set => this.SetProperty(() => this.LastUpdate, value);
        }

        public double Close
        {
            get => this.GetProperty(() => this.Close);
            set => this.SetProperty(() => this.Close, value);
        }

        public DateTime LastTrainingDate
        {
            get => this.GetProperty(() => this.LastTrainingDate);
            set => this.SetProperty(() => this.LastTrainingDate, value);
        }

        public TrainingSession TrainingSession
        {
            get => this.GetProperty(() => this.TrainingSession);
            set => this.SetProperty(() => this.TrainingSession, value);
        }
    }
}