using System;
using System.Diagnostics;

namespace Wism.Client.Common
{
    public class WismLogger : IWismLogger
    {
        public void LogError(string message)
        {
            Log.WriteLine(Log.TraceLevel.Error, message);
            Console.WriteLine($"[{Log.TraceLevel.Error}] {message}");
        }

        public void LogWarning(string message)
        {
            Log.WriteLine(Log.TraceLevel.Warning, message);
            Console.WriteLine($"[{Log.TraceLevel.Warning}] {message}");
        }

        public void LogInformation(string message)
        {
            Log.WriteLine(Log.TraceLevel.Information, message);
            Console.WriteLine($"[{Log.TraceLevel.Information}] {message}");
        }
    }
}
