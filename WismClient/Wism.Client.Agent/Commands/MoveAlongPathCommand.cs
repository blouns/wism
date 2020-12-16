using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public class MoveAlongPathCommand : ArmyCommand
    {
        private IList<Tile> path;
        public int X { get; set; }
        public int Y { get; set; }

        public MoveAlongPathCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
        }

        public override ActionState Execute()
        {
            return armyController.MoveOneStep(Armies, World.Current.Map[X, Y], ref path, out _);                        
        }
    }
}
