using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public class Terrain : MapObject
    {
        private TerrainInfo info;

        public override string DisplayName { get => info.DisplayName; }

        public override char Symbol { get => info.Symbol; }

        public static Terrain Create(TerrainInfo info)
        {
            return new Terrain(info);
        }

        private Terrain(TerrainInfo info)
        {
            this.info = info;
        }
    }

    
}
