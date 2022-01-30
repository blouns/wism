using System;
using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public class PlayerInformationMapping : IInformationMapping
    {
        string[] labels = new string[]
        {
            "Name:",
            "Income:",
            "Cities:",
            "Gold:",
            "Turn:",
            "Command:",
            "Upkeep:",
            ""
        };

        public bool CanMapSubject(Tile subject)
        {
            return subject == null;
        }

        public void GetLabelValuePair(int index, Tile subject, out string label, out string value)
        {
            if (index >= this.labels.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            label = this.labels[index];
            value = GetLabelValuePair(index);
        }

        private string GetLabelValuePair(int index)
        {
            string value;
            Player player = Game.Current.GetCurrentPlayer();

            switch (index)
            {
                case 0: // Name
                    value = player.Clan.DisplayName;
                    break;
                case 1: // Income
                    value = player.GetIncome().ToString();
                    break;
                case 2: // Cities
                    value = player.GetCities().Count.ToString();
                    break;
                case 3: // Gold
                    value = player.Gold.ToString();
                    break;
                case 4: // Turn
                    value = player.Turn.ToString();
                    break;
                case 5: // Command
                    value = "None";     // TODO: Decide how to handle this
                    break;
                case 6: // Upkeep
                    value = player.GetUpkeep().ToString();
                    break;
                default:
                    value = "";
                    break;
            }

            return value;
        }
    }
}
