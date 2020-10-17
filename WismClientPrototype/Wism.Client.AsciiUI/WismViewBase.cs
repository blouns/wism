using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Wism.Client.AsciiUi
{
    /// <summary>
    /// Template base class for a generic UI
    /// </summary>
    public abstract class WismViewBase
    {
        private ILogger logger;

        public WismViewBase(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger<WismViewBase>();
        }
        public async Task RunAsync()
        {
            logger.LogInformation("WISM View successfully started");
            
            int lastId = 0;
            while (true)
            {
                System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1));
                logger.LogInformation("WISM View heartbeat...");            
                
                // Game loop
                Draw();
                HandleInput();
                DoTasks(ref lastId);
            }
        }

        protected abstract void DoTasks(ref int lastId);

        protected abstract void HandleInput();

        protected abstract void Draw();
    }
}
