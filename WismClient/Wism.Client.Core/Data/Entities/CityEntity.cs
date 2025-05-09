﻿using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public class CityEntity
    {
        [DataMember] public int Id { get; set; }

        [DataMember] public int X { get; set; }

        [DataMember] public int Y { get; set; }

        [DataMember] public string CityShortName { get; set; }

        [DataMember] public int Defense { get; set; }

        [DataMember] public string ClanShortName { get; set; }

        [DataMember] public ArmyInTrainingEntity ArmyInTraining { get; set; }

        [DataMember] public ArmyInTrainingEntity[] ArmiesToDeliver { get; set; }

        [DataMember] public ProductionEntity ProductionInfo { get; set; }
    }
}