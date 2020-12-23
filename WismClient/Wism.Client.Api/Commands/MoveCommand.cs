using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
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

        public override ActionState Execute()
        {
            IList<Tile> path = null;
            return armyController.MoveOneStep(Armies, World.Current.Map[X, Y], ref path, out _);
        }
    }
}
