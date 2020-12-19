using System;
using Wism.Client.Core.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Api.Commands
{
    public class StartTurnCommand : Command
    {
        private readonly GameController gameController;

        public StartTurnCommand(GameController gameController)
            : base()
        {
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            this.gameController = gameController;
        }

        public override ActionState Execute()
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
