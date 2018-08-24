using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public abstract class Terrain : MapObject
    {
        private Coordinate position = new Coordinate(-1, -1);
        private Affiliation affliation;

        public Coordinate Position { get => position; }
        public Affiliation Affliation { get => affliation; set => affliation = value; }
    }

    public sealed class TerrainVoid : Terrain
    {
        public override string GetDisplayName()
        {
            return "The void";
        }
    }

    public sealed class TerrainMountain : Terrain
    {
        public override string GetDisplayName()
        {
            return "Mountain";
        }
    }

    public sealed class TerrainMeadow : Terrain
    {
        public override string GetDisplayName()
        {
            return "Meadow";
        }
    }

    public sealed class TerrainCastle : Terrain
    {
        public override string GetDisplayName()
        {
            return "Castle";
        }
    }
}
