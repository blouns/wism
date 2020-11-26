using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BranallyGames.Wism.Repository.Entities
{
    public class GameEntityBase
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string ShortName { get; set; }

        [MaxLength(128)]
        public string DisplayName { get; set; }
    }
}
