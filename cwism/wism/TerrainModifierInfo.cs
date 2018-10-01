using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BranallyGames.Wism
{
    [DataContract]
    public class TerrainModifierInfo : ICustomizable
    {
        private const string fileName = "TerrainModifierInfo_Template.json";

        public string FileName { get => fileName; }

        public static readonly string FilePattern = fileName;

        // TODO: Implement
    }
}
