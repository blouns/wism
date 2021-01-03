using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Model.Commands
{
    public class MoveCommandDto : CommandDto
    {
        public int X { get; set; }

        public int Y { get; set; }

        public ArmyDto Army { get; set; }
    }
}
