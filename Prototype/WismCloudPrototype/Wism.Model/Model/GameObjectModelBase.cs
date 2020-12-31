using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BranallyGames.Wism.API.Model
{
    public class GameObjectModelBase : GameObjectNoIdModelBase
    {
        /// <summary>
        /// The GUID of the world
        /// </summary>
        [Required(ErrorMessage = "An ID is requried.")]
        [RegularExpression("^(?!(00000000-0000-0000-0000-000000000000)$)", ErrorMessage = "Cannot use empty Guid.")]
        public Guid Id { get; set; }
    }
}
