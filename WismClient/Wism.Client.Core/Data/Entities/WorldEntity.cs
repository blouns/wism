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

        /// <summary>
        /// Tiles is a flattened 2D array (x + y * xBounds) == (x, y)
        /// </summary>
        [DataMember]
        public TileEntity[] Tiles { get; set; }

        [DataMember]
        public int MapXUpperBound { get; set; }

        [DataMember]
        public int MapYUpperBound { get; set; }

    }
}