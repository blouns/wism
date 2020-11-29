using System;
using System.Collections.Generic;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
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

        public override bool Execute()
        {
            if (!armyController.TryAttack(Armies, World.Current.Map[X, Y]))
            {
                return false;
            }

            // Attack successful; move into the location
            // TODO: Check game state to ensure attacking army is selected
            bool success = armyController.TryMove(Armies, World.Current.Map[X, Y]);

            return success;
        }
    }
}
