using System;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Agent.Commands
{
    public class EndTurnCommand : Command
    {
        private readonly GameController gameController;

        public EndTurnCommand(GameController gameController, Player player)
            : base(player)
        {
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            this.gameController = gameController;
        }

        public override bool Execute()
        {
            return gameController.EndTurn(Game.Current);
        }
    }
}
