using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    // TODO: Implement locations (ruins, tombs, libraries, temples, sages)
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
            return false;
        }

        public void GetLabelValuePair(int index, Tile subject, out string label, out string value)
        {
            throw new NotImplementedException();
        }
    }
}
