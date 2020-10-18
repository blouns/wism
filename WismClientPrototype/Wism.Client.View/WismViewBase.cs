using BranallyGames.Wism;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Wism.Client.View
{
    /// <summary>
    /// Template base class for a generic UI
    /// </summary>
    public abstract class WismViewBase
    {
        private ILogger logger;

        protected Army selectedArmy;

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
            SetupWorld();

            try
            {
                int lastId = 0;
                while (true)
                {
                    // Game loop
                    Draw();
                    HandleInput();
                    DoTasks(ref lastId);

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                throw;
            }
        }

        protected abstract void DoTasks(ref int lastId);

        protected abstract void HandleInput();

        protected abstract void Draw();

        private void SetupWorld()
        {
            World.CreateDefaultWorld();
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            World.Current.Players[1].ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 1]);
            this.selectedArmy = World.Current.Players[0].GetArmies()[0];
        }
    }
}
