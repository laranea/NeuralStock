namespace twentySix.NeuralStock.Core.Services
{
    using System;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;

    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Repository.Hierarchy;

    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [Export(typeof(ILoggingService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LoggingService : ILoggingService
    {
        private static ILog _logger;
        private static string _logDirectory;

        [ImportingConstructor]
        public LoggingService()
        {
            XmlConfigurator.Configure();
            _logger = LogManager.GetLogger(typeof(LoggingService));

            var fileAppender = LogManager.GetRepository().GetAppenders().First(appender => appender is FileAppender) as FileAppender;
            var logFilename = Path.GetFileName(fileAppender?.File);

            if (fileAppender != null)
            {
                fileAppender.File = Path.Combine(ApplicationHelper.GetAppDataFolder(), logFilename ?? "application.log");
                fileAppender.ActivateOptions();
            }
        }

        public void Debug(object message)
        {
            _logger.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            _logger.Debug(message, exception);
        }

        public void Info(object message)
        {
            _logger.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            _logger.Info(message, exception);
        }

        public void Warn(object message)
        {
            _logger.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            _logger.Warn(message, exception);
        }

        public void Error(object message)
        {
            _logger.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            _logger.Error(message, exception);
        }

        public string GetLogDirectory()
        {
            if (string.IsNullOrEmpty(_logDirectory))
            {
                var logFile = ((FileAppender)((Hierarchy)LogManager.GetRepository()).Root.Appenders[0]).File;
                if (!string.IsNullOrEmpty(logFile))
                {
                    if (File.Exists(logFile))
                    {
                        _logDirectory = new FileInfo(logFile).Directory?.FullName;
                    }
                }
            }

            return _logDirectory;
        }
    }
}