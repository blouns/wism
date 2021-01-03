using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BranallyGames.Wism.API.Model
{
    /// <summary>
    /// Model used for getting a world
    /// </summary>
    public class PlayerModel : GameObjectModelBase
    {
        /// <summary>
        /// True this player is a human
        /// </summary>
        public bool IsHuman { get; set; }
    }
}
