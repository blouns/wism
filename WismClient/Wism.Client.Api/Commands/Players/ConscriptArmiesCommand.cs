using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Modules.Infos;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

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

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = nameof(ConscriptArmiesCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = Tile?.ToString(),
                TargetPosition = Tile != null
                    ? new PositionDto { X = Tile.X, Y = Tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "ArmyCount", ArmiesResult?.Count ?? 0 },
                    { "ArmyTypes", string.Join(", ", ArmyKinds.Select(k => k.ShortName)) },
                    { "Terrain", Tile?.Terrain?.ToString() ?? "Unknown" },
                    { "Tile", Tile?.ToString() ?? "Unknown" }
                }
            };
        }
    }
}