using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class AssemblyListEntity
    {
        [DataMember] public AssemblyEntity[] Strategies { get; set; }
    }
}