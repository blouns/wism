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
    public abstract class ViewBase
    {
        private ILogger logger;
        private readonly ArmyController armyController;

        public ViewBase(ILoggerFactory loggerFactory, ArmyController armyController)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger<ViewBase>();
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
        }

        public async Task RunAsync()
        {
            logger.LogInformation("WISM successfully started");

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
            var heroTile = World.Current.Map[1, 1];
            Game.Current.Players[0].HireHero(heroTile);
            Game.Current.Players[0].ConscriptArmy(
                ModFactory.FindArmyInfo("HeavyInfantry"),
                heroTile);
            Game.Current.Players[0].ConscriptArmy(
                ModFactory.FindArmyInfo("Pegasus"),
                heroTile);
            Game.Current.Players[0].ConscriptArmy(
                ModFactory.FindArmyInfo("Pegasus"),
                heroTile);

            Game.Current.Players[0].ConscriptArmy(
                ModFactory.FindArmyInfo("Pegasus"),
                heroTile);

            Game.Current.Players[0].ConscriptArmy(
                ModFactory.FindArmyInfo("Pegasus"),
                heroTile);


            // Set the player's selected army to a default for testing
            armyController.SelectArmy(heroTile.Armies);

            // Create an opponent with a light infantry for testing
            var enemyTile = World.Current.Map[2, 2];
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile);
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("Cavalry"),
                enemyTile);
        }
    }
}
