using System;
using System.ComponentModel.DataAnnotations;

namespace Wism.Client.Model
{
    public abstract class MapObjectBaseDto
    {
        [Key]
        public Guid Guid { get; set; }

        public string ID { get; set; }

        public string DisplayName { get; set; }

        public int X { get; set; }

        public int Y { get; set; }
    }
}
