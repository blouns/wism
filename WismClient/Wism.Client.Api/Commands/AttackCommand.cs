using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    [Obsolete]
    public class AttackCommand : ArmyCommand
    {
        public int X { get; set; }
        public int Y { get; set; }

        public AttackCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
        }

        public override ActionState Execute()
        {
            if (!armyController.TryAttack(Armies, World.Current.Map[X, Y]))
            {
                return ActionState.Failed;
            }
          
            IList<Tile> path = null;
            _ = armyController.MoveOneStep(Armies, World.Current.Map[X, Y], ref path, out _);   // Move
            _ = armyController.MoveOneStep(Armies, World.Current.Map[X, Y], ref path, out _);   // Arrive

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} attack ({World.Current.Map[X, Y]}";
        }
    }
}
