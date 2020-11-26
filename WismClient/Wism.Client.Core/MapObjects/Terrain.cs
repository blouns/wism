using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Terrain : MapObject
    {
        private TerrainInfo info;

        public int MovementCost { get; set; }

        public override string DisplayName { get => Info.DisplayName; }

        public override string ShortName { get => Info.ShortName; }
        public TerrainInfo Info 
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindTerrainInfo(this.ShortName);
                return info;
            }
        }

        private Terrain(TerrainInfo info)
        {
            this.info = info ?? throw new System.ArgumentNullException(nameof(info));
            this.MovementCost = info.Movement;
        }

        public static Terrain Create(TerrainInfo info)
        {
            return new Terrain(info);
        }

        public bool CanTraverse(bool canWalk, bool canFloat, bool canFly)
        {
            return ((canWalk && Info.AllowWalk) ||
                    (canFloat && Info.AllowFloat) ||
                    (canFly && Info.AllowFlight));
        }
    }
}