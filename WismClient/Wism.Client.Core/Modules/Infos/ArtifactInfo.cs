using System.Runtime.Serialization;

namespace Wism.Client.Modules.Infos
{
    [DataContract]
    public class ArtifactInfo
    {
        public static readonly string FileName = "Artifact.json";

        [DataMember] public string DisplayName { get; set; } = "Sword";

        [DataMember] public string ShortName { get; set; } = "Sword";

        [DataMember] public int CommandBonus { get; set; }

        [DataMember] public int CombatBonus { get; set; }

        [DataMember] public string Interaction { get; set; }
    }
}