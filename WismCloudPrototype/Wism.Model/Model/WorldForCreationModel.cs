using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BranallyGames.Wism.API.Model
{
    /// <summary>
    /// Model used for creating new worlds
    /// </summary>
    public class WorldForCreationModel : GameObjectNoIdModelBase
    {
        public IEnumerable<PlayerModel> Players { get; set; }
    }
}
