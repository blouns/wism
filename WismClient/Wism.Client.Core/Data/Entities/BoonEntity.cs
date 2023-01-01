using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class BoonEntity
    {
        [DataMember] public string BoonTypeName { get; set; }

        [DataMember] public string BoonAssemblyName { get; set; }

        [DataMember] public string ArtifactShortName { get; set; }

        [DataMember] public string AlliesShortName { get; set; }
    }
}