using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Wism.Client.Data.Entities
{
    public abstract class Command
    {
        public Command()
        {
            ArmyCommands = new List<ArmyCommand>();
        }

        public int Id { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public List<ArmyCommand> ArmyCommands { get; set; }
    }
}