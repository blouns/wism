using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class RandomEntity
    {
        [DataMember]
        public int[] SeedArray { get; set; }

        [DataMember]
        public int Seed { get; set; }
    }
}
