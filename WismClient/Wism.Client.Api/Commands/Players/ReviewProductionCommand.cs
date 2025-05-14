using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.Core.Armies;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Commands.Players
{
    public class ReviewProductionCommand : Command
    {
        private readonly CityController cityController;

        public ReviewProductionCommand(CityController cityController, Core.Player player)
            : base(player)
        {
            this.cityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
        }

        public List<ArmyInTraining> ArmiesProducedResult { get; private set; }
        public List<ArmyInTraining> ArmiesDeliveredResult { get; private set; }

        protected override ActionState ExecuteInternal()
        {
            var state = ActionState.Failed;

            if (this.cityController.TryGetProducedArmies(this.Player,
                    out var armiesProduced,
                    out var armiesDelivered))
            {
                this.ArmiesProducedResult = armiesProduced;
                this.ArmiesDeliveredResult = armiesDelivered;
                state = ActionState.Succeeded;
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} reviewing production";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = nameof(ReviewProductionCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = "ProductionSystem",
                TargetPosition = null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "ProducedCount", ArmiesProducedResult?.Count ?? 0 },
                    { "DeliveredCount", ArmiesDeliveredResult?.Count ?? 0 },
                    { "ProducedTypes", string.Join(", ", ArmiesProducedResult?.Select(a => a.ArmyInfo?.ShortName) ?? Enumerable.Empty<string>()) },
                    { "DeliveredTypes", string.Join(", ", ArmiesDeliveredResult?.Select(a => a.ArmyInfo?.ShortName) ?? Enumerable.Empty<string>()) }
                }
            };
        }
    }
}