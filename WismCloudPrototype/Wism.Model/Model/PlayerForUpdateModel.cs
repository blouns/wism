using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BranallyGames.Wism.API.Model
{
    /// <summary>
    /// Model used for updating worlds
    /// </summary>
    public class PlayerForUpdateModel : GameObjectNoIdModelBase
    {
        /// <summary>
        /// True if player is a human
        /// </summary>
        public bool IsHuman { get; set; }
    }
}
