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

        public bool EndTurn(Player player)
        {
            logger.LogInformation($"{player} has ended their turn.");
            return Game.Current.EndTurn(player);
        }
    }
}
