using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Commands.Players
{
    public class ConscriptArmiesCommand : Command
    {
        private readonly PlayerController playerController;

        public ConscriptArmiesCommand(PlayerController playerController, Core.Player player, Tile tile,
            List<ArmyInfo> armyKinds)
            : base(player)
        {
            this.playerController = playerController ?? throw new ArgumentNullException(nameof(playerController));
            this.Tile = tile ?? throw new ArgumentNullException(nameof(tile));
            this.ArmyKinds = armyKinds ?? throw new ArgumentNullException(nameof(armyKinds));

            if (armyKinds.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(armyKinds), "Must be at least on army kind.");
            }
        }

        public Tile Tile { get; set; }

        public List<ArmyInfo> ArmyKinds { get; set; }

        public List<MapObjects.Army> ArmiesResult { get; private set; }

        protected override ActionState ExecuteInternal()
        {
            var state = this.playerController.ConscriptArmies(
                this.Player, this.ArmyKinds, this.Tile, out var armies);

            if (state == ActionState.Succeeded)
            {
                this.ArmiesResult = new List<MapObjects.Army>(armies);
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} conscripting armies";
        }
    }
}