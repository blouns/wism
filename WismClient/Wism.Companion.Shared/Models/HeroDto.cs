using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Companion.Shared.Models
{
    public class HeroDto
    {
        public string Name { get; set; }
        public PositionDto Position { get; set; }
        public int Health { get; set; }
        public string Owner { get; set; }
    }
}
