using System;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class TerrainInformationMapping : IInformationMapping
    {
        string[] labels = new string[]
        {
            "Name:",
            "Terrain:",
            "Lands:",
            "",
            "",
            "Command:",
            "",
            ""
        };

        public bool CanMapSubject(Tile subject)
        {
            return (subject != null) && (subject.Terrain != null);
        }

        public void GetLabelValuePair(int index, Tile subject, out string label, out string value)
        {
            if (index >= this.labels.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            label = this.labels[index];
            value = GetLabelValuePair(index, subject);
        }

        private string GetLabelValuePair(int index, Tile subject)
        {
            string value;
            Player player = Game.Current.GetCurrentPlayer();
            Terrain terrain = subject.Terrain;

            switch (index)
            {
                case 0: // Name
                    value = player.Clan.DisplayName;
                    break;
                case 1: // Terrain
                    value = terrain.DisplayName;
                    break;
                case 2: // Lands
                    value = "Illuria";  // TODO: Add "lands" feature to map (or cut)
                    break;
                case 5: // Command
                    value = "Info";
                    break;
                default:
                    value = "";
                    break;
            }

            return value;
        }
    }
}
