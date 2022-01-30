using System;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class CityInformationMapping : IInformationMapping
    {
        string[] labels = new string[]
        {
            "Name:",
            "City:",
            "Defence:",
            "",
            "",
            "Command:",
            "Owner:",
            "Income"
        };

        public bool CanMapSubject(Tile subject)
        {
            return (subject != null) && subject.HasCity();
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
            City city = subject.City;

            switch (index)
            {
                case 0: // Name
                    value = player.Clan.DisplayName;
                    break;
                case 1: // City
                    value = city.DisplayName;
                    break;
                case 2: // Defence
                    value = city.Defense.ToString();
                    break;
                case 5: // Command
                    value = "Info";
                    break;
                case 6: // Owner
                    value = city.Clan.DisplayName;
                    break;
                case 7: // Income
                    value = city.Income.ToString();
                    break;
                default:
                    value = "";
                    break;
            }

            return value;
        }
    }
}
