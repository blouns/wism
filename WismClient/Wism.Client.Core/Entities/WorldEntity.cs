using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class WorldEntity
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public LocationEntity[] Locations { get; set; }

        [DataMember]
        public CityEntity[] Cities { get; set; }

        [DataMember]
        public TileEntity[,] Tiles { get; set; }
    }
}