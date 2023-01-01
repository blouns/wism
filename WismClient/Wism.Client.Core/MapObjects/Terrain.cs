using System;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Terrain : MapObject
    {
        private TerrainInfo info;

        private Terrain(TerrainInfo info)
        {
            this.info = info ?? throw new ArgumentNullException(nameof(info));
            this.MovementCost = info.Movement;
            this.DisplayName = info.DisplayName;
        }

        public int MovementCost { get; set; }

        public override string ShortName => this.Info.ShortName;

        public TerrainInfo Info
        {
            get
            {
                if (this.info == null)
                {
                    this.info = MapBuilder.FindTerrainInfo(this.ShortName);
                }

                return this.info;
            }
        }

        public static Terrain Create(TerrainInfo info)
        {
            return new Terrain(info);
        }

        public bool CanTraverse(bool canWalk, bool canFloat, bool canFly)
        {
            return (canWalk && this.Info.AllowWalk) ||
                   (canFloat && this.Info.AllowFloat) ||
                   (canFly && this.Info.AllowFlight);
        }
    }
}