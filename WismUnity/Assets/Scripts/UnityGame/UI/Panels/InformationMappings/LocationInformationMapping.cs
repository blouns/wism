using Assets.Scripts.Managers;
using System;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class LocationInformationMapping : IInformationMapping
    {
        string[] labels = new string[]
        {
            "Name:",
            "Location:",
            "Lands:",
            "",
            "",
            "Command:",
            "Status:",
            ""
        };

        public bool CanMapSubject(Tile subject)
        {
            return (subject != null) && subject.HasLocation();
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
            Location location = subject.Location;
            string worldName = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<GameManager>().WorldName;

            switch (index)
            {
                case 0: // Name
                    value = player.Clan.DisplayName;
                    break;
                case 1: // Location
                    value = location.DisplayName;
                    break;
                case 2: // Lands
                    value = worldName;
                    break;
                case 5: // Command
                    value = "Info";
                    break;
                case 6: // Status
                    value = GetStatus(location);
                    break;
                default:
                    value = "";
                    break;
            }

            return value;
        }

        private string GetStatus(Location location)
        {
            string status;

            switch (location.Kind)
            {
                case "Library":
                    status = "Library";
                    break;
                case "Sage":
                    status = "Sage";
                    break;
                case "Temple":
                    status = "Healers";
                    break;
                case "Ruins":
                case "Tomb":
                default:
                    status = (location.Searched) ? "Explored" : "Unexplored";
                    break;
            }

            return status;
        }
    }
}
