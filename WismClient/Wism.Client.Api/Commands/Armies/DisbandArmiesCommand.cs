using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Commands.Armies
{
    public enum DisbandRejectionReason
    {
        None,
        AlreadyExecuted,
        NoActiveGame,
        StaleGame,
        InvalidGameState,
        WrongPlayer,
        EmptySelection,
        InvalidArmy,
        DuplicateArmy,
        StaleArmy,
        UnrelatedVisitor,
        HeroesOnly
    }

    /// <summary>
    /// A side-effect-free preview. Lists are snapshots; execution always validates again.
    /// </summary>
    public sealed class DisbandArmiesPreview
    {
        internal DisbandArmiesPreview(DisbandRejectionReason rejectionReason,
            IEnumerable<Army> armiesToDisband, IEnumerable<Hero> heroesToPreserve)
        {
            this.RejectionReason = rejectionReason;
            this.ArmiesToDisband = new List<Army>(armiesToDisband).AsReadOnly();
            this.HeroesToPreserve = new List<Hero>(heroesToPreserve).AsReadOnly();
        }

        public bool CanExecute => this.RejectionReason == DisbandRejectionReason.None;
        public DisbandRejectionReason RejectionReason { get; }
        public IReadOnlyList<Army> ArmiesToDisband { get; }
        public IReadOnlyList<Hero> HeroesToPreserve { get; }
    }

    /// <summary>
    /// Removes ordinary armies through the engine lifecycle, never living heroes.
    /// Uses Command rather than ArmyCommand so empty and null batches can be rejected safely.
    /// </summary>
    /// <remarks>
    /// Game snapshots support the resulting state. Command history retains the original
    /// requested IDs, including rejected entries; this is not an executable replay format.
    /// </remarks>
    public sealed class DisbandArmiesCommand : Command
    {
        private readonly Game game;
        private readonly World world;
        private readonly Player owner;
        private bool executed;

        public DisbandArmiesCommand(ArmyController armyController, List<Army> armies)
        {
            if (armyController == null)
                throw new ArgumentNullException(nameof(armyController));

            this.Armies = armies == null ? null : new List<Army>(armies).AsReadOnly();
            this.owner = armies?.FirstOrDefault()?.Player;
            this.Player = this.owner;
            if (Game.IsInitialized() && Game.Current.Players != null && Game.Current.Players.Count > 0)
            {
                this.game = Game.Current;
                this.world = World.Current;
            }
        }

        public IReadOnlyList<Army> Armies { get; }

        /// <summary>
        /// Valid only in Ready or SelectedArmy on the owner's turn. Selected visitors
        /// must match the engine's current selection; arbitrary visitors are not removable.
        /// </summary>
        public DisbandArmiesPreview Preview()
        {
            if (this.executed)
                return Reject(DisbandRejectionReason.AlreadyExecuted);
            if (!Game.IsInitialized() || this.game == null)
                return Reject(DisbandRejectionReason.NoActiveGame);
            if (this.game != Game.Current || this.world != World.Current)
                return Reject(DisbandRejectionReason.StaleGame);
            if (this.game.GameState != GameState.Ready && this.game.GameState != GameState.SelectedArmy)
                return Reject(DisbandRejectionReason.InvalidGameState);
            if (this.Armies == null || this.Armies.Count == 0)
                return Reject(DisbandRejectionReason.EmptySelection);
            if (this.Armies.Any(army => army == null || army.IsDead || army.Tile == null))
                return Reject(DisbandRejectionReason.InvalidArmy);

            var player = this.game.GetCurrentPlayer();
            if (player == null || player.IsDead || this.owner != player || this.Player != player)
                return Reject(DisbandRejectionReason.WrongPlayer);
            if (this.Armies.Any(army => army.Player != player))
                return Reject(DisbandRejectionReason.WrongPlayer);
            if (this.Armies.Distinct().Count() != this.Armies.Count)
                return Reject(DisbandRejectionReason.DuplicateArmy);

            var roster = player.GetArmies();
            var selected = this.game.GetSelectedArmies();
            var map = this.world.Map;
            foreach (var army in this.Armies)
            {
                var tile = army.Tile;
                if (map == null || tile.X < 0 || tile.Y < 0 ||
                    tile.X >= map.GetLength(0) || tile.Y >= map.GetLength(1) ||
                    map[tile.X, tile.Y] != tile || roster.Count(candidate => candidate == army) != 1)
                    return Reject(DisbandRejectionReason.StaleArmy);

                var residents = tile.Armies?.Count(candidate => candidate == army) ?? 0;
                var visitors = tile.VisitingArmies?.Count(candidate => candidate == army) ?? 0;
                if (residents + visitors != 1)
                    return Reject(DisbandRejectionReason.StaleArmy);
                if (visitors == 1 &&
                    (selected == null || !selected.Any(candidate => candidate == army) ||
                     selected.Count != tile.VisitingArmies.Count ||
                     selected.Distinct().Count() != selected.Count ||
                     selected.Any(candidate => candidate == null || candidate.Tile != tile ||
                         tile.VisitingArmies.Count(visitor => visitor == candidate) != 1)))
                    return Reject(DisbandRejectionReason.UnrelatedVisitor);
            }

            var ordinary = this.Armies.Where(army => !(army is Hero)).ToList();
            var heroes = this.Armies.OfType<Hero>().ToList();
            return new DisbandArmiesPreview(
                ordinary.Count == 0 ? DisbandRejectionReason.HeroesOnly : DisbandRejectionReason.None,
                ordinary, heroes);
        }

        protected override ActionState ExecuteInternal()
        {
            var preview = this.Preview();
            if (!preview.CanExecute)
                return ActionState.Failed;

            // Kill performs the canonical tile, player roster and selection bookkeeping.
            foreach (var army in preview.ArmiesToDisband)
                army.Kill();

            if (this.game.GameState == GameState.SelectedArmy && !this.game.ArmiesSelected())
                this.game.Transition(GameState.Ready);
            this.executed = true;
            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: disband {this.Armies?.Count ?? 0} armies (preserve heroes)";
        }

        private static DisbandArmiesPreview Reject(DisbandRejectionReason reason)
        {
            return new DisbandArmiesPreview(reason, Array.Empty<Army>(), Array.Empty<Hero>());
        }
    }
}
