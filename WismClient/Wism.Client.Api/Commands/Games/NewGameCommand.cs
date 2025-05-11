using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Data.Entities;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Commands.Games
{
    public class NewGameCommand : Command
    {
        public NewGameCommand(GameController gameController, GameEntity settings)
        {
            this.GameController = gameController ?? throw new ArgumentNullException(nameof(gameController));
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public GameController GameController { get; }

        public GameEntity Settings { get; }

        protected override ActionState ExecuteInternal()
        {
            return this.GameController.NewGame(this.Settings);
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var settings = this.Settings;

            return new CommandExecutedEvent
            {
                CommandType = nameof(NewGameCommand),
                ActorId = Player?.Clan?.ShortName ?? "System",
                TargetId = settings?.GameState.ToString(),
                TargetPosition = null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Created", settings?.Timestamp.ToString("o") ?? "Unknown" },
                    { "InitialPlayerIndex", settings?.CurrentPlayerIndex ?? -1 },
                    { "PlayerCount", settings?.Players?.Length ?? 0 },
                    { "GameState", settings?.GameState.ToString() ?? "Unknown" },
                    { "LastArmyId", settings?.LastArmyId ?? -1 },
                    { "InitSuccess", result == ActionState.Succeeded }
                }
            };
        }
    }
}