using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public class TileEntity
    {
        [DataMember] public int X { get; set; }

        [DataMember] public int Y { get; set; }

        [DataMember] public string TerrainShortName { get; set; }

        [DataMember] public ArtifactEntity[] Items { get; set; }

        [DataMember] public string CityShortName { get; set; }

        [DataMember] public string LocationShortName { get; set; }

        [DataMember(EmitDefaultValue = false)] public string TowerOwnerClanShortName { get; set; }

        [DataMember(EmitDefaultValue = false)] public bool IsTowerRazed { get; set; }

        [DataMember] public int[] ArmyIds { get; set; }

        [DataMember] public int[] VisitingArmyIds { get; set; }
    }
}