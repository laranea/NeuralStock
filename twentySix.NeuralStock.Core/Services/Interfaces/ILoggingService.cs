namespace twentySix.NeuralStock.Core.Services.Interfaces
{
    using System;

    public interface ILoggingService
    {
        void Debug(object message);

        void Debug(object message, Exception exception);

        void Error(object message);

        void Error(object message, Exception exception);

        string GetLogDirectory();

        void Info(object message);

        void Info(object message, Exception exception);

        void Warn(object message);

        void Warn(object message, Exception exception);
    }
}