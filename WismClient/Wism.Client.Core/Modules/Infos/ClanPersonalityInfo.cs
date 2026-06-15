using System.Runtime.Serialization;

namespace Wism.Client.Modules.Infos
{
    [DataContract]
    public sealed class ClanPersonalityInfo
    {
        [DataMember] public string Profile { get; set; } = "balanced";

        [DataMember] public double Aggressive { get; set; } = 1.0;

        [DataMember] public double Raider { get; set; } = 1.0;

        [DataMember] public double Explorer { get; set; } = 1.0;

        [DataMember] public double Defender { get; set; } = 1.0;

        [DataMember] public double Economy { get; set; } = 1.0;

        [DataMember] public double Opportunist { get; set; } = 1.0;

        public static ClanPersonalityInfo Balanced { get; } = new ClanPersonalityInfo();
    }
}
