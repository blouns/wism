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

        public override ActionState Execute()
        {
            if (!armyController.TryAttack(Armies, World.Current.Map[X, Y]))
            {
                return ActionState.Failed;
            }

            // Attack successful; move into the location            
            bool success = armyController.TryMove(Armies, World.Current.Map[X, Y]);

            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}
