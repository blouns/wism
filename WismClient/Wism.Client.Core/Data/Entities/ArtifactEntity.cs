using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public class ArtifactEntity
    {
        [DataMember] public int Id { get; set; }

        [DataMember] public string ArtifactShortName { get; set; }

        [DataMember] public int X { get; set; }

        [DataMember] public int Y { get; set; }

        [DataMember] public int PlayerIndex { get; set; }
    }
}