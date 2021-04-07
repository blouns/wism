using System;
using Wism.Client.Core.Controllers;
using Wism.Client.Core;

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
            gameController.StartTurn(Game.Current);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {Player.Clan} start turn";
        }
    }
}
