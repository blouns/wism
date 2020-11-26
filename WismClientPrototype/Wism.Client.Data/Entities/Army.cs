using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Wism.Client.Data.Entities
{
    public class Army
    {
        public Army()
        {
            ArmyCommands = new List<ArmyCommand>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int HitPoints { get; set; }

        public int Strength { get; set; }

        public List<ArmyCommand> ArmyCommands { get; set; }

    }
}
