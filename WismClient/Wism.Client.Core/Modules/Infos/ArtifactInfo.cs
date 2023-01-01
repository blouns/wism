using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class ArtifactInfo
    {
        public static readonly string FileName = "Artifact.json";

        [DataMember]
        public string DisplayName { get; set; } = "Sword";

        [DataMember]
        public string ShortName { get; set; } = "Sword";

        [DataMember]
        public int CommandBonus { get; set; } = 0;

        [DataMember]
        public int CombatBonus { get; set; } = 0;

        [DataMember]
        public string Interaction { get; set; }
    }
}
