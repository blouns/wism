using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Commands.Locations
{
    public sealed class BuildTowerCommand : Command
    {
        public const int BuildCost = 100;
        private readonly List<Army> builders;

        public BuildTowerCommand(List<Army> armies, Tile tile)
            : base(ResolvePlayer(armies))
        {
            this.builders = new List<Army>(armies);
            this.Tile = tile ?? throw new ArgumentNullException(nameof(tile));
        }

        public Tile Tile { get; }
        public string FailureReason { get; private set; }

        public bool CanBuild()
        {
            this.FailureReason = null;
            if (!this.CanExecuteInCurrentState || this.Player.IsDead ||
                Game.Current.GetCurrentPlayer() != this.Player)
                return Reject("You can only build during your own turn.");

            var map = World.Current.Map;
            if (this.Tile.X < 0 || this.Tile.Y < 0 || this.Tile.X >= map.GetLength(0) ||
                this.Tile.Y >= map.GetLength(1) || map[this.Tile.X, this.Tile.Y] != this.Tile)
                return Reject("The building site is no longer in this world.");

            if (this.builders.Count == 0 || this.builders.Any(army => army == null || army.IsDead ||
                army.Player != this.Player || army.Tile != this.Tile ||
                !this.Player.GetArmies().Contains(army) || !this.Tile.ContainsArmies(new List<Army> { army })))
                return Reject("Your builder army must still be stationed on this tile.");

            if (this.Tile.IsTowerRazed || this.Tile.City != null || this.Tile.Location != null ||
                this.Tile.Terrain?.ShortName != "Grass" || this.Tile.HasVisitingArmies() ||
                this.Tile.GetAllArmies().Any(army => army.Player != this.Player))
                return Reject("Towers can only be built on clear plains, never on ruins or existing structures.");

            if (this.Player.Gold < BuildCost)
                return Reject($"Building a tower costs {BuildCost} gp. You do not have enough gold.");

            return true;
        }

        protected override ActionState ExecuteInternal()
        {
            if (!CanBuild()) return ActionState.Failed;
            this.Player.Gold -= BuildCost;
            this.Tile.Terrain = MapBuilder.TerrainKinds["Tower"];
            this.Tile.TowerOwnerClanShortName = this.Player.Clan.ShortName;
            return ActionState.Succeeded;
        }

        public override string ToString() => $"tower ({this.Tile.X},{this.Tile.Y}) build";

        private bool Reject(string reason)
        {
            this.FailureReason = reason;
            return false;
        }

        private static Player ResolvePlayer(List<Army> armies)
        {
            if (armies == null || armies.Count == 0 || armies[0]?.Player == null)
                throw new ArgumentException("At least one army with a player is required.", nameof(armies));
            return armies[0].Player;
        }
    }
}
