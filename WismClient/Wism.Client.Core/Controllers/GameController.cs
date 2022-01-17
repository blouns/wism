using System;
using Wism.Client.Common;
using Wism.Client.Entities;
using Wism.Client.Factories;

namespace Wism.Client.Core.Controllers
{
    public class GameController
    {
        private readonly ILogger logger;

        public GameController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        public void EndTurn()
        {
            EndTurn(Game.Current);
        }

        public void EndTurn(Game game)
        {
            logger.LogInformation(
                $"{game.GetCurrentPlayer()} ended their turn.");
            
            game.EndTurn();
        }

        public void StartTurn()
        {
            StartTurn(Game.Current);
        }

        public ActionState NewGame(GameEntity settings)
        {
            logger.LogInformation("Creating new game...");
            try
            {
                // Load into current game
                _ = GameFactory.Create(settings);
            }
            catch
            {
                logger.LogError("Game creation failed.");
                throw;
            }

            logger.LogInformation("New game successfully created.");

            return ActionState.Succeeded;
        }

        public ActionState LoadSnapshot(GameEntity snapshot)
        {
            logger.LogInformation("Loading game snapshot...");
            try
            {
                // Load into current game
                _ = GameFactory.Load(snapshot);                
            }
            catch
            {
                logger.LogError("Snapshot load failed.");
                throw;
            }

            logger.LogInformation("Snapshot successfully loaded.");

            return ActionState.Succeeded;
        }

        public void StartTurn(Game game)
        {
            logger.LogInformation(
                $"{game.GetCurrentPlayer()} is starting their turn.");

            game.StartTurn();
        }
    }
}
