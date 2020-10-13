using System;
using System.ComponentModel.DataAnnotations;

namespace Wism.Client.Data.Entities
{
    public class Army
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int HitPoints { get; set; }

        public int Strength { get; set; }

    }
}
