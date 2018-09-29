using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Terrain : MapObject
    {
        private char symbol;

        private TerrainInfo info;

        public override string DisplayName { get => Info.DisplayName; }

        public override char Symbol { get => Info.Symbol; set => this.symbol = value; }
        
        internal TerrainInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindTerrainInfo(symbol);
                return info;
            }
        }

        public static Terrain Create(TerrainInfo info)
        {
            return new Terrain(info);
        }

        private Terrain(TerrainInfo info)
        {
            this.info = info;
        }

        public Terrain()
        {
        }

        public bool CanTraverse(bool canWalk, bool canFloat, bool canFly)
        {
            return ((canWalk && Info.AllowWalk) ||
                    (canFloat && Info.AllowFloat) ||
                    (canFloat && Info.AllowFlight));
        }
    }
}
