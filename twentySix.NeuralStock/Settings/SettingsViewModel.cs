namespace twentySix.NeuralStock.Settings
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Threading.Tasks;

    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;

    using twentySix.NeuralStock.Common;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [POCOViewModel]
    public class SettingsViewModel : ComposedViewModelBase, IDataErrorInfo
    {
        protected SettingsViewModel()
        {
            this.LoadSettings();
        }

        public virtual NeuralStockSettings Settings { get; set; }

        public string Error => null;

        public virtual INavigationService NavigationService => null;

        [Import]
        protected IPersistenceService PersistenceService { get; set; }

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public void GoBack()
        {
            this.NavigationService?.GoBack(false);
        }

        protected override async void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();
            await this.SaveSettings();
        }

        private async void LoadSettings()
        {
            this.Settings = await this.PersistenceService.GetSettings() ?? NeuralStockSettings.GetDefault();
        }

        private async Task SaveSettings()
        {
            await this.PersistenceService.SaveSettings(this.Settings);
        }
    }
}