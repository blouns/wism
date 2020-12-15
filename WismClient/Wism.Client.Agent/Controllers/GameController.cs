using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;

namespace Wism.Client.Agent.Controllers
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

            this.logger = loggerFactory.CreateLogger<GameController>();
        }

        public void EndTurn()
        {
            EndTurn(Game.Current);
        }

        public void EndTurn(Game game)
        {
            logger.LogInformation(
                $"{game.GetCurrentPlayer()} has ended their turn.");
            
            game.EndTurn();
        }

        public void StartTurn()
        {
            StartTurn(Game.Current);
        }

        public void StartTurn(Game game)
        {
            logger.LogInformation(
                $"{game.GetCurrentPlayer()} is starting their turn.");

            game.StartTurn();
        }
    }
}
