using System;
using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public class RandomEntity
    {
        [DataMember] public int[] SeedArray { get; set; }

        [DataMember] public int Seed { get; set; }

        [DataMember] public Random Random { get; set; }
    }
}