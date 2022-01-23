using System;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public class StartTurnCommand : Command
    {
        private readonly GameController gameController;

        public StartTurnCommand(GameController gameController, Player player)
            : base(player)
        {
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            this.gameController = gameController;
        }

        protected override ActionState ExecuteInternal()
        {
            this.gameController.StartTurn(Game.Current);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} start turn";
        }
    }
}
