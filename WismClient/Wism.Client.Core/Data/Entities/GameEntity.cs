﻿using System;
using System.Runtime.Serialization;
using Wism.Client.Core;

namespace Wism.Client.Entities
{
    [DataContract]
    public class GameEntity
    {
        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public RandomEntity Random { get; set; }

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

        // Other fields
        [DataMember]
        public int LastArmyId { get; set; }
    }
}