using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class AssemblyEntity
    {
        [DataMember] public string TypeName { get; set; }

        [DataMember] public string AssemblyName { get; set; }
    }
}