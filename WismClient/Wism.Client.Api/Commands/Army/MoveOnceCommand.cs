using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Commands.Army
{
    public class MoveOnceCommand : ArmyCommand
    {
        private IList<Tile> path;

        public MoveOnceCommand(ArmyController armyController, List<MapObjects.Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public IList<Tile> Path
        {
            get => this.path;
            set => this.path = value;
        }

        protected override ActionState ExecuteInternal()
        {
            return this.armyController.MoveOneStep(this.Armies, World.Current.Map[this.X, this.Y], ref this.path,
                out _);
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} move to ({World.Current.Map[this.X, this.Y]}";
        }
    }
}