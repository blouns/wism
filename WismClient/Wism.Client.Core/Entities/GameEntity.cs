using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Wism.Client.Core;

namespace Wism.Client.Entities
{
    [DataContract]
    public class GameEntity
    {
        [DataMember]
        public int RandomSeed { get; set; }

        [DataMember]
        public int CurrentPlayerIndex { get; set; }

        [DataMember]
        public GameState GameState { get; set; }

        [DataMember]
        public int[] SelectedArmyIds { get; set; }

        [DataMember]
        public PlayerEntity[] Players { get; set; }

        [DataMember]
        public WorldEntity World { get; set; }

        [DataMember]
        public WarStrategyEntity WarStrategy { get; set; }
    }
}
