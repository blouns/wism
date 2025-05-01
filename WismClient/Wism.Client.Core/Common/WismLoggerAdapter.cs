using Microsoft.Extensions.Logging;
using System;

namespace Wism.Client.Common
{
    public class WismLoggerAdapter<T> : ILogger<T>
    {
        private readonly IWismLogger wismLogger;

        public WismLoggerAdapter(IWismLogger wismLogger)
        {
            this.wismLogger = wismLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Error:
                    wismLogger.LogError(message);
                    break;
                case LogLevel.Warning:
                    wismLogger.LogWarning(message);
                    break;
                default:
                    wismLogger.LogInformation(message);
                    break;
            }
        }
    }
}
