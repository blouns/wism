using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Data.Entities
{
    public class ArmyMoveCommand : Command
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
