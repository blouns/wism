using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Terrain : MapObject
    {
        private TerrainInfo info;
        private int movementCost;

        public override string DisplayName { get => Info.DisplayName; set => Info.DisplayName = value; }

        public override string ID { get => Info.ID; }
        
        public TerrainInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindTerrainInfo(this.ID);
                return info;
            }
        }

        public int MovementCost { get => movementCost; set => movementCost = value; }

        public static Terrain Create(TerrainInfo info)
        {
            return new Terrain(info);
        }

        private Terrain(TerrainInfo info)
        {
            this.info = info;
            this.movementCost = info.Movement;
        }

        public bool CanTraverse(bool canWalk, bool canFloat, bool canFly)
        {
            return ((canWalk && Info.AllowWalk) ||
                    (canFloat && Info.AllowFloat) ||
                    (canFly && Info.AllowFlight));
        }
    }
}
