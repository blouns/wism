using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class LocationEntity
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public int X { get; set; }

        [DataMember]
        public int Y { get; set; }

        [DataMember]
        public string LocationShortName { get; set; }

        [DataMember]
        public bool Searched { get; set; }

        [DataMember]
        public BoonEntity Boon { get; set; }

        [DataMember]
        public string Monster { get; set; }
    }
}