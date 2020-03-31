namespace twentySix.NeuralStock.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;

    using twentySix.NeuralStock.Core.Services;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    public static class ApplicationHelper
    {
        private const int MaxStoredExceptions = 100;
        private static bool _isRunning;
        private static ILoggingService _logger;

        static ApplicationHelper()
        {
            CompanyName = "twentySix";
            Title = "NeuralStock";

            var exeAssemblyNumber = Assembly.GetEntryAssembly()?.GetName().Version;
            AssemblyVersionNumber = exeAssemblyNumber?.ToString(4);
        }

        public static List<Exception> ExceptionsRaised { get; } = new List<Exception>();

        public static TaskScheduler TaskScheduler { get; set; }

        public static CompositionContainer CurrentCompositionContainer { get; private set; }

        public static string AssemblyVersionNumber { get; }

        public static string Title { get; }

        public static string CompanyName { get; }

        public static string GetAssemblyTypeLocation(Type type)
        {
            string fullPath = Assembly.GetAssembly(type).Location;
            return Path.GetDirectoryName(fullPath);
        }

        public static string GetAppDataFolder()
        {
            var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyName, Title);

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            return appFolder;
        }

        public static void StartUp(ILoggingService loggingService, CompositionContainer compositionContainer)
        {
            if (_isRunning)
            {
                return;
            }

            _logger = loggingService;
            CurrentCompositionContainer = compositionContainer;

            // log startup
            Task.Factory.StartNew(LogStartUp);
            _isRunning = true;
        }

        public static void ShutDown(string reason)
        {
            try
            {
                _logger.Info(reason);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public static bool HandleExceptions(Exception exception)
        {
            bool rethrow = false;

            if (ExceptionsRaised.Count < MaxStoredExceptions)
            {
                ExceptionsRaised.Add(exception);
            }
            else
            {
                rethrow = true;
            }

            HandleNoPresentationException(exception);

            return rethrow;
        }

        public static void HandleNoPresentationException(Exception exception)
        {
            if (_logger == null)
            {
                _logger = new LoggingService();
            }

            _logger.Error($"Exception raised at: {DateTime.Now}");
            _logger.Error($"Exception Message  : {exception.Message}");
        }

        public static void ShowStartupError()
        {
            if (ExceptionsRaised.Any())
            {
                MessageBox.Show(
                    ExceptionsRaised.Aggregate(
                        string.Empty,
                        (current, exception) => current + exception.Message + Environment.NewLine),
                    "Error starting framework",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ShutDown("Startup errors encountered");
            }
        }

        private static void LogStartUp()
        {
            _logger.Info("--------------------------------");
            _logger.Info($"- Application : {Title}");
            _logger.Info($"- Version     : {AssemblyVersionNumber}");
            _logger.Info("--------------------------------");
        }
    }
}