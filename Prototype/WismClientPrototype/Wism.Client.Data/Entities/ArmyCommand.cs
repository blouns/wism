using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Data.Entities
{
    public class ArmyCommand
    {
        public Guid ArmyId { get; set; }
        public int CommandId { get; set; }

        public Army Army { get; set; }
        public Command Command { get; set; }

    }
}
