using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Api.Commands
{
    public class ConscriptArmiesCommand : Command
    {
        private readonly PlayerController playerController;

        public Tile Tile { get; set; }

        public List<ArmyInfo> ArmyKinds { get; set; }

        public List<Army> ArmiesResult { get; private set; }

        public ConscriptArmiesCommand(PlayerController playerController, Player player, Tile tile, List<ArmyInfo> armyKinds)
            : base(player)
        {
            this.playerController = playerController ?? throw new ArgumentNullException(nameof(playerController));
            Tile = tile ?? throw new ArgumentNullException(nameof(tile));
            ArmyKinds = armyKinds ?? throw new ArgumentNullException(nameof(armyKinds));

            if (armyKinds.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(armyKinds), "Must be at least on army kind.");
            }
        }

        protected override ActionState ExecuteInternal()
        {
            var state = playerController.ConscriptArmies(
                this.Player, this.ArmyKinds, this.Tile, out List<Army> armies);

            if (state == ActionState.Succeeded)
            {
                this.ArmiesResult = new List<Army>(armies);
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {Player.Clan} conscripting armies";
        }
    }
}
