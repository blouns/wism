using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Companion.Shared.Events;

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

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = nameof(EndTurnCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = null,
                TargetPosition = null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Player", Player?.Clan?.ShortName ?? "Unknown" },
                    { "Turn", Player?.Turn ?? -1 }
                }
            };
        }
    }
}