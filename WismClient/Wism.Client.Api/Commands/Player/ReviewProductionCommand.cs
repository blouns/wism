using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Core.Armies;

namespace Wism.Client.Commands.Player
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
    }
}