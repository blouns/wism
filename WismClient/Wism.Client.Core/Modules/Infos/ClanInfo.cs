using System;
using System.Runtime.Serialization;

namespace Wism.Client.Modules.Infos
{
    [DataContract]
    public class ClanInfo
    {
        public static readonly string FileName = "Clan.json";

        [DataMember] public string Color = "(255, 0, 0)";

        [DataMember] public string ShortName = "xx";

        [DataMember] public string DisplayName { get; set; } = "Clan Name";

        public static ClanInfo GetClanInfo(string id)
        {
            var info = ModFactory.FindClanInfo(id);
            if (info == null)
            {
                throw new InvalidOperationException("No such type found.");
            }

            return info;
        }
    }
}