using System;
using System.Runtime.Serialization;

namespace Wism.Client.Modules.Infos
{
    [DataContract]
    public class ClanInfo
    {
        public static readonly string FileName = "Clan.json";

        [DataMember] public string Color = "(255, 0, 0)";

        [DataMember] public string PrimaryColor { get; set; }
        [DataMember] public string SecondaryColor { get; set; }
        // Optional reuse of an installed clan's artwork; identity remains ShortName.
        [DataMember] public string VisualClanName { get; set; }
        [DataMember] public bool Playable { get; set; } = true;

        public string ArtworkClanName => string.IsNullOrWhiteSpace(VisualClanName) ? ShortName : VisualClanName;

        [OnDeserializing]
        private void SetLegacyDefaults(StreamingContext context)
        {
            // DataContractJsonSerializer does not run constructors or field initializers.
            Playable = true;
            Color = "(255, 0, 0)";
        }

        public static bool TryParseRgb(string value, out byte red, out byte green, out byte blue)
        {
            red = green = blue = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.Trim();
            if (!text.StartsWith("(") || !text.EndsWith(")")) return false;
            var parts = text.Substring(1, text.Length - 2).Split(',');
            return parts.Length == 3 && byte.TryParse(parts[0].Trim(), out red)
                && byte.TryParse(parts[1].Trim(), out green) && byte.TryParse(parts[2].Trim(), out blue);
        }

        [DataMember] public string ShortName = "xx";

        [DataMember] public string DisplayName { get; set; } = "Clan Name";

        [DataMember] public int StartingGold { get; set; } = 1000;

        [DataMember] public ClanPersonalityInfo Personality { get; set; }

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
