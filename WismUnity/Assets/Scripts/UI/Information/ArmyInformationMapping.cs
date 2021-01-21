using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class ArmyInformationMapping : IInformationMapping
    {
        string[] labels = new string[]
        {
            "Name:",
            "Move:",
            "Army:",
            "",
            "",
            "Command:",
            "Strength:",
            ""
        };

        public bool CanMapSubject(Tile subject)
        {
            // Do not provide info on other clan's armies
            return (subject != null) && subject.HasVisitingArmies();
        }

        public void GetLabelValuePair(int index, Tile subject, out string label, out string value)
        {
            if (index >= labels.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            label = labels[index];
            value = GetLabelValuePair(index, subject);
        }

        private string GetLabelValuePair(int index, Tile subject)
        {
            string value;
            Player player = Game.Current.GetCurrentPlayer();
            List<Army> armies = subject.VisitingArmies;

            switch (index)
            {
                case 0: // Name
                    value = player.Clan.DisplayName;
                    break;
                case 1: // Move
                    value = armies.Min(a => a.MovesRemaining).ToString();
                    break;
                case 2: // Army
                    value = (armies.Count == 1) ? armies[0].DisplayName : "Group";                    
                    break;
                case 5: // Command
                    value = "Move Army";
                    break;
                case 6: // Strength
                    value = (armies.Count == 1) ? armies[0].Strength.ToString() : "-";
                    break;
                default:
                    value = "";
                    break;
            }

            return value;
        }
    }
}
