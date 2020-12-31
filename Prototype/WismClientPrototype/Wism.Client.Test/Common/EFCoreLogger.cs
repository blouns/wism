using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Test.Common
{
    internal class EFCoreLogger : ILogger
    {
        private readonly Action<string> efCoreLogAction;
        private readonly LogLevel logLevel;

        public EFCoreLogger(Action<string> efCoreLogAction, LogLevel logLevel)
        {
            this.efCoreLogAction = efCoreLogAction;
            this.logLevel = logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= this.logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.efCoreLogAction($"LogLevel: {logLevel}, {state}");
        }
    }
}
