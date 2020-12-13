using System;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Agent.Commands
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

        public override ActionState Execute()
        {
            gameController.StartTurn(Game.Current);

            return ActionState.Succeeded;
        }
    }
}
