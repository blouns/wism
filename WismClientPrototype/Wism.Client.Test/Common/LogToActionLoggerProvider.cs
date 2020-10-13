using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Test.Common
{
    internal class LogToActionLoggerProvider : ILoggerProvider
    {
        private readonly Action<string> efCoreLogAction;
        private readonly LogLevel logLevel;

        public LogToActionLoggerProvider(
            Action<string> efCoreLogAction, 
            LogLevel logLevel = LogLevel.Information)
        {
            this.efCoreLogAction = efCoreLogAction;
            this.logLevel = logLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new EFCoreLogger(efCoreLogAction, logLevel);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
