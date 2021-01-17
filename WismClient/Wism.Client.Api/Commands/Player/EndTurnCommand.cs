using System;
using Wism.Client.Core.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Api.Commands
{
    public class EndTurnCommand : Command
    {
        private readonly GameController gameController;

        public EndTurnCommand(GameController gameController, Player player)
            : base()
        {
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            this.gameController = gameController;
            this.Player = player;
        }

        protected override ActionState ExecuteInternal()
        {
            gameController.EndTurn(Game.Current);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {Player.Clan} end turn";
        }
    }
}
