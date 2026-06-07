using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.Core.Armies;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Commands.Players
{
    public class RenewProductionCommand : Command
    {
        private readonly CityController cityController;

        public RenewProductionCommand(CityController cityController, Core.Player player,
            ReviewProductionCommand reviewProductionCommand)
            : base(player)
        {
            this.cityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ReviewProductionCommand = reviewProductionCommand ??
                                           throw new ArgumentNullException(nameof(reviewProductionCommand));

            // Default to renewing all production
            if (reviewProductionCommand.ArmiesProducedResult != null)
            {
                this.ArmiesToRenew = new List<ArmyInTraining>(reviewProductionCommand.ArmiesProducedResult);
            }
        }

        public ReviewProductionCommand ReviewProductionCommand { get; }

        public List<ArmyInTraining> ArmiesToRenew { get; set; }

        protected override ActionState ExecuteInternal()
        {
            var state = ActionState.Failed;

            if (this.ReviewProductionCommand.Result == ActionState.Succeeded)
            {
                this.ArmiesToRenew = this.ArmiesToRenew ??
                    new List<ArmyInTraining>(this.ReviewProductionCommand.ArmiesProducedResult ?? Enumerable.Empty<ArmyInTraining>());
                state = this.cityController.RenewProduction(this.Player, this.ArmiesToRenew);
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} renewing production";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = nameof(RenewProductionCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = null,
                TargetPosition = null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "RenewedCount", ArmiesToRenew?.Count ?? 0 },
                    { "UnitTypes", string.Join(", ", ArmiesToRenew?.Select(a => a.ArmyInfo?.ShortName) ?? Enumerable.Empty<string>()) },
                    { "FromReview", ReviewProductionCommand?.GetType().Name ?? "Unknown" }
                }
            };
        }
    }
}
