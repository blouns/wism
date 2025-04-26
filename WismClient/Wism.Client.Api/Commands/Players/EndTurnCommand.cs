using System;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.Players
{
    public class EndTurnCommand : Command
    {
        private readonly GameController gameController;

        public EndTurnCommand(GameController gameController, Core.Player player)
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
            this.gameController.EndTurn(Core.Game.Current);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} end turn";
        }
    }
}