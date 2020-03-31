namespace twentySix.NeuralStock
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;

    using DevExpress.Xpf.Core;

    using twentySix.NeuralStock.Core.Helpers;

    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.DispatcherUnhandledException += this.App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += this.TaskScheduler_UnobservedTaskException;

            try
            {
                ApplicationHelper.TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

                var bootstrapper = new Bootstrapper();
                bootstrapper.Run();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var item in ex.LoaderExceptions)
                {
                    ApplicationHelper.HandleExceptions(item);
                }
            }
            catch (Exception ex)
            {
                ApplicationHelper.HandleExceptions(ex);
            }
        }

        private void OnAppStartup_UpdateThemeName(object sender, StartupEventArgs e)
        {
            ApplicationThemeHelper.UpdateApplicationThemeName();
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception exception = e.Exception.Flatten();
            ApplicationHelper.HandleExceptions(exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            ApplicationHelper.HandleExceptions(exception);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception exception = e.Exception;
            ApplicationHelper.HandleExceptions(exception);
        }
    }
}