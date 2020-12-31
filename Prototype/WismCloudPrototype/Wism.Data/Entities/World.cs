using System;
using System.Collections.Generic;
using System.Text;

namespace BranallyGames.Wism.Repository.Entities
{
    public class World : GameEntityBase
    {
        public ICollection<Player> Players { get; set; }
            = new List<Player>();
    }
}
