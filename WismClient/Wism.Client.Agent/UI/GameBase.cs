using System;
using System.Threading.Tasks;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.Modules;
using Wism.Client.Common;

namespace Wism.Client.Agent
{
    /// <summary>
    /// Template base class for a generic UI
    /// </summary>
    public abstract class GameBase
    {
        private ILogger logger;
        private readonly ArmyController armyController;
        private int lastId;

        public const int DefaultGameSpeed = 100;
        public const int DefaultAttackSpeed = 750;

        public int GameSpeed { get; set; }

        public int LastId { get => lastId; set => lastId = value; }

        public GameBase(ILoggerFactory loggerFactory, ControllerProvider controllerProvider)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            if (controllerProvider is null)
            {
                throw new ArgumentNullException(nameof(controllerProvider));
            }

            logger = loggerFactory.CreateLogger();
            this.armyController = controllerProvider.ArmyController;
            this.GameSpeed = DefaultGameSpeed;
        }

        public async Task RunAsync()
        {
            logger.LogInformation("WISM successfully started");

            try
            {
                CreateTestGame();
                
                while (true)
                {
                    // Game loop
                    Draw();
                    HandleInput();
                    DoTasks(ref lastId);

                    await Task.Delay(GameSpeed);
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
            string worldName = "SearchWorld";

            Game.CreateDefaultGame(worldName);
            var world = World.Current;
            var map = world.Map;

            // Create a default hero for testing
            var heroTile = map[1, 1];
            Game.Current.Players[0].HireHero(heroTile);
            Game.Current.Players[0].ConscriptArmy(
                ModFactory.FindArmyInfo("HeavyInfantry"),
                heroTile);
            Game.Current.Players[0].ConscriptArmy(
                ModFactory.FindArmyInfo("Pegasus"),
                heroTile);

            // Set the player's selected army to a default for testing
            armyController.SelectArmy(heroTile.Armies);

            // Create an opponent for testing
            var enemyTile1 = map[3, 3];
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile1);
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile1);
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile1);
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile1);
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile1);

            var enemyTile2 = map[3, 2];
            Game.Current.Players[1].ConscriptArmy(
                ModFactory.FindArmyInfo("LightInfantry"),
                enemyTile2);

            // Add cities and locations
            MapBuilder.AddCitiesFromWorldPath(world, worldName);
            MapBuilder.AddLocationsFromWorldPath(world, worldName);
            MapBuilder.AllocateBoons(world.GetLocations());

        }
    }
}
