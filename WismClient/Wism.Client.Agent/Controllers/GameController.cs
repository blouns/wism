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

        public bool EndTurn()
        {
            return EndTurn(Game.Current);
        }

        public bool EndTurn(Game game)
        {
            logger.LogInformation(
                $"{game.GetCurrentPlayer()} has ended their turn.");
            
            return game.EndTurn();
        }

        public bool StartTurn()
        {
            return StartTurn(Game.Current);
        }

        public bool StartTurn(Game game)
        {
            logger.LogInformation(
                $"{game.GetCurrentPlayer()} is starting their turn.");

            return game.StartTurn();
        }
    }
}
