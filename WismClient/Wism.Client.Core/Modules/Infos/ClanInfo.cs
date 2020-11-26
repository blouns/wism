using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class ClanInfo
    {
        public static readonly string FileName = "Clan.json";

        [DataMember]
        public string ShortName = "xx";

        [DataMember]
        public string DisplayName { get; set; } = "Clan Name";

        [DataMember]
        public string Color = "(255, 0, 0)";

        public static ClanInfo GetClanInfo(string id)
        {
            ClanInfo info = ModFactory.FindClanInfo(id);
            if (info == null)
                throw new InvalidOperationException("No such type found.");

            return info;
        }
    }
}
