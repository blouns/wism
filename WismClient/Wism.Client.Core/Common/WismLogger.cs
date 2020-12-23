using System;

namespace Wism.Client.Common
{
    public class WismLogger : ILogger
    {
        public void LogError(string message)
        {
            Log.WriteLine(Log.TraceLevel.Error, message);
        }

        public void LogInformation(string message)
        {
            Log.WriteLine(Log.TraceLevel.Information, message);
        }

        public void LogWarning(string message)
        {
            Log.WriteLine(Log.TraceLevel.Warning, message);
        }
    }
}
