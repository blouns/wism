using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace wism
{
    public static class Log
    {
        private static readonly string logFile = @"logs\wism_" + Guid.NewGuid();

        public enum TraceLevel : int
        {
            Information = 0,
            Warning,
            Error,
            Critical
        }            

        static Log()
        {
            Debug.Listeners.Add(new TextWriterTraceListener(logFile));
            Debug.AutoFlush = true;
        }

        public static void WriteLine(TraceLevel level, string message, params object[] args)
        {
            WriteLineIf(level, message);
        }

        public static void WriteLineIf(TraceLevel level, string message, params object[] args)
        {
            // Log always for now
            Debug.WriteLine(
                String.Format("{0}, {1}, \"{2}\"",
                    DateTime.Now.ToShortTimeString(),
                    level.ToString(),
                    String.Format(message, args)));
        }
    }
}
