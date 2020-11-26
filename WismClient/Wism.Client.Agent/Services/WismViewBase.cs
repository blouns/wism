using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Agent
{
    /// <summary>
    /// Template base class for a generic UI
    /// </summary>
    public abstract class WismViewBase
    {
        private ILogger logger;
        protected List<Army> selectedArmies;

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

            try
            {
                CreateDefaultGame();

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

        /// <summary>
        /// For testing purposes only. Creates a default world for testing.
        /// </summary>
        private void CreateDefaultGame()
        {
            Game.CreateDefaultGame();
            
            // Create a default hero for testing
            Game.Current.Players[0].HireHero(World.Current.Map[1, 1]);

            // Set the player's selected army to a default for testing
            this.selectedArmies = new List<Army>();
            this.selectedArmies.Add(Game.Current.Players[0].GetArmies()[0]);

            // Create an opponent with a light infantry for testing
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                World.Current.Map[2, 2]);
        }
    }
}
