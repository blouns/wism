using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wism.Client.Core.Controllers;
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

        public ViewBase(ILoggerFactory loggerFactory, ControllerProvider controllerProvider)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            if (controllerProvider is null)
            {
                throw new ArgumentNullException(nameof(controllerProvider));
            }

            logger = loggerFactory.CreateLogger<ViewBase>();
            this.armyController = controllerProvider.ArmyController;
        }

        public async Task RunAsync()
        {
            logger.LogInformation("WISM successfully started");

            try
            {
                CreateTestGame();

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
                Console.WriteLine(ex.ToString());
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
        private void CreateTestGame()
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

            // Set the player's selected army to a default for testing
            armyController.SelectArmy(heroTile.Armies);

            // Create an opponent with a light infantry for testing
            var enemyTile1 = World.Current.Map[3, 3];
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile1);
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("Cavalry"),
                enemyTile1);

            var enemyTile2 = World.Current.Map[3, 4];
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile2);

            // Add cities
            MapBuilder.AddCity(World.Current.Map, 1, 1, "Marthos", "Sirians");
            MapBuilder.AddCity(World.Current.Map, 3, 3, "BanesCitadel", "LordBane");
        }
    }
}
