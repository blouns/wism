﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace wism
{
    [DataContract]
    public class TerrainInfo : ICustomizable
    {
        public string FileName { get => "Terrain_Template.json"; }

        public static readonly string FilePattern = "Terrain_*.json";

        [DataMember]
        public string Type = "Terrain";

        [DataMember]
        public string DisplayName { get; set; } = "Display Name";

        [DataMember]
        private string imageFileName = "image.jpg";

        [DataMember]
        public char Symbol { get; set; } = 'x';

        [DataMember]
        public bool AllowFlight { get; set; } = false;

        [DataMember]
        public bool AllowFloat { get; set; } = false;

        [DataMember]
        public bool AllowWalk { get; set; } = false;
    }
}