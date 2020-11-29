using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public class MoveCommand : ArmyCommand
    {
        public int X { get; set; }
        public int Y { get; set; }

        public MoveCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
        }

        public override bool Execute()
        {
            return armyController.TryMove(Armies, World.Current.Map[X, Y]);
        }
    }
}
