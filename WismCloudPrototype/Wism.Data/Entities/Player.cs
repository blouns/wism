using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BranallyGames.Wism.Repository.Entities
{
    public class Player : GameEntityBase
    {
        public bool IsHuman { get; set; }

        [ForeignKey("WorldId")]
        public World World { get; set; }

        public Guid WorldId { get; set; }
    }
}
