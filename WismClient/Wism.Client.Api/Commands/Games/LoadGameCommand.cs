using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Data.Entities;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Commands.Games
{
    public class LoadGameCommand : Command
    {
        public LoadGameCommand(GameController gameController, GameEntity snapshot)
        {
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            this.GameController = gameController;
            this.Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public GameController GameController { get; }

        public GameEntity Snapshot { get; }

        [Newtonsoft.Json.JsonIgnore]
        public string FailureReason { get; private set; }

        protected override ActionState ExecuteInternal()
        {
            this.FailureReason = null;
            try
            {
                return this.GameController.LoadSnapshot(this.Snapshot);
            }
            catch (Exception error)
            {
                // The loader has restored the prior campaign. Record a terminal
                // result so command consumers do not retry the bad save each frame.
                this.FailureReason = error.ToString();
                return ActionState.Failed;
            }
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var snapshot = this.Snapshot;

            return new CommandExecutedEvent
            {
                CommandType = nameof(LoadGameCommand),
                ActorId = Player?.Clan?.ShortName ?? "System",
                TargetId = snapshot?.GameState.ToString(),
                TargetPosition = null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "SnapshotCreated", snapshot?.Timestamp.ToString("o") ?? "Unknown" },
                    { "CurrentPlayerIndex", snapshot?.CurrentPlayerIndex ?? -1 },
                    { "PlayerCount", snapshot?.Players?.Length ?? 0 },
                    { "GameState", snapshot?.GameState.ToString() ?? "Unknown" },
                    { "LastArmyId", snapshot?.LastArmyId ?? -1 },
                    { "LoadSuccess", result == ActionState.Succeeded }
                }
            };
        }
    }
}
