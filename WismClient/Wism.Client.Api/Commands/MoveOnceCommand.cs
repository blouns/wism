using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class MoveOnceCommand : ArmyCommand
    {
        private IList<Tile> path;
        public int X { get; set; }
        public int Y { get; set; }

        public MoveOnceCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
        }

        public override ActionState Execute()
        {
            return armyController.MoveOneStep(Armies, World.Current.Map[X, Y], ref path, out _);
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} move to ({World.Current.Map[X, Y]}";
        }
    }
}
