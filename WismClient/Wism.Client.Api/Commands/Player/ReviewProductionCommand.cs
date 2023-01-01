using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public class ReviewProductionCommand : Command
    {
        private readonly CityController cityController;

        public ReviewProductionCommand(CityController cityController, Player player)
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
    }
}