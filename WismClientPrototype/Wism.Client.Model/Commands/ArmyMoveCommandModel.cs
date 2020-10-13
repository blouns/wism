using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Model.Commands
{
    public class ArmyMoveCommandModel : GameCommandModel
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
