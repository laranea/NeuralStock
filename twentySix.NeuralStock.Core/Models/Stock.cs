namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Linq;

    using Data.Countries;

    using DevExpress.Mvvm;

    using DTOs;

    using Helpers;

    [Export]
    [DebuggerDisplay("Symbol = {" + nameof(Symbol) + "}")]  
    public class Stock : BindableBase, IDataErrorInfo, IDisposable
    {
        public Stock()
        {
            this.Symbol = string.Empty;
            this.Name = string.Empty;
            this.HistoricalData = null;
            this.Country = new Singapore();
        }

        ~Stock()
        {
            this.Dispose(false);
        }

        [Key]
        public int Id { get; set; }

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

        public ICountry Country
        {
            get => this.GetProperty(() => this.Country);
            set => this.SetProperty(() => this.Country, value);
        }

        public HistoricalData HistoricalData
        {
            get => this.GetProperty(() => this.HistoricalData);
            set => this.SetProperty(() => this.HistoricalData, value);
        }

        public string Error => null;

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static Stock FromDTO(StockDTO dto, HistoricalDataDTO historicalDataDTO)
        {
            if (dto == null)
            {
                return null;
            }

            // available countries
            var availableCountries = ApplicationHelper.CurrentCompositionContainer.GetExportedValues<ICountry>();

            return new Stock
            {
                Id = dto.Id,
                Symbol = dto.Symbol,
                Name = dto.Name,
                Country = availableCountries.SingleOrDefault(x => x.Id == dto.CountryId),
                HistoricalData = HistoricalData.FromDTO(historicalDataDTO)
            };
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public StockDTO GetDTO()
        {
            return new StockDTO
            {
                Id = this.Id,
                Symbol = this.Symbol,
                Name = this.Name,
                CountryId = this.Country.Id,
                HistoricalDataId = this.HistoricalData.Id
            };
        }

        public int GetUniqueId()
        {
            return (this.Symbol + this.Country?.Name).GetHashCode();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            this.HistoricalData = null;
        }
    }
}