using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BranallyGames.Wism.API.Model
{
    /// <summary>
    /// Common base for game model objects
    /// </summary>
    public abstract class GameObjectNoIdModelBase
    { 
        /// <summary>
        /// The short name or moniker of the object 
        /// </summary>
        [Required(ErrorMessage = "A short name is requried.")]
        [MaxLength(64, ErrorMessage = "The short name must be less that 64 characters.")]
        public string ShortName { get; set; }

        /// <summary>
        /// The display name of the object
        /// </summary>
        [Required(ErrorMessage = "A display name is requried.")]
        [MaxLength(128, ErrorMessage = "The display name must be less that 128 characters.")]
        public string DisplayName { get; set; }
    }
}
