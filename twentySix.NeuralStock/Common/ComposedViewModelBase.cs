namespace twentySix.NeuralStock.Common
{
    using System.ComponentModel.Composition;

    using DevExpress.Mvvm;

    using twentySix.NeuralStock.Core.Helpers;

    public class ComposedViewModelBase : NavigationViewModelBase
    {
        protected ComposedViewModelBase()
        {
            ApplicationHelper.CurrentCompositionContainer.ComposeParts(this);
        }
    }
}