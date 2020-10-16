using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Model.Commands
{
    public class ArmyMoveCommandDto : CommandDto
    {
        public ArmyDto Army { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
