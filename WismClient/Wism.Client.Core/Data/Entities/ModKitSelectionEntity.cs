using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public sealed class ModKitSelectionEntity
    {
        [DataMember] public int SchemaVersion { get; set; } = 1;

        [DataMember] public string WismVersion { get; set; }

        [DataMember] public string ProfileId { get; set; }

        [DataMember] public string ProfileVersion { get; set; }

        [DataMember] public string[] PackIds { get; set; }

        [DataMember] public string[] PackVersions { get; set; }

        [DataMember] public string World { get; set; }

        [DataMember] public string ContentFingerprint { get; set; }
    }
}
