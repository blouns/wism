using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BranallyGames.Wism.API.Model
{
    public class GameActionResultModel
    {
        /// <summary>
        /// ID in chronological order of execution
        /// </summary>
        [Key]
        public int SequenceId { get; set; }

        /// <summary>
        /// Game Object before the state change
        /// </summary>
        public GameObjectModelBase Before { get; set; }

        /// <summary>
        /// Game Object after the state change
        /// </summary>
        public IEnumerable<GameObjectModelBase> After { get; set; }

        /// <summary>
        /// Date-time the action-result was executed
        /// </summary>
        public DateTime ExecutedDateTime { get; set; }
    }
}
