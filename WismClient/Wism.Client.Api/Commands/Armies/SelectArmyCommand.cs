using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Commands.Armies
{
    public class SelectArmyCommand : ArmyCommand
    {
        public SelectArmyCommand(ArmyController armyController, List<MapObjects.Army> armies)
            : base(armyController, armies)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            if (this.Armies == null || this.Armies.Count == 0)
            {
                return ActionState.Failed;
            }

            var player = Game.Current.GetCurrentPlayer();
            if (player == null || this.Armies.Any(army => army?.Player != player))
            {
                return ActionState.Failed;
            }

            var tile = this.Armies[0].Tile;
            if (tile == null ||
                this.Armies.Any(army => army?.Tile != tile) ||
                tile.HasVisitingArmies() && !IsExactVisitingSelection(tile, this.Armies) ||
                !tile.HasVisitingArmies() && !tile.ContainsArmies(this.Armies))
            {
                return ActionState.Failed;
            }

            this.ArmyController.SelectArmy(this.Armies);

            return ActionState.Succeeded;
        }

        private static bool IsExactVisitingSelection(Core.Tile tile, List<MapObjects.Army> armies)
        {
            return tile.ContainsVisitingArmies(armies) &&
                   tile.VisitingArmies.Count == armies.Count;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} select";
        }
    }
}
