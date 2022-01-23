using System;
using System.Collections.Generic;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class Clan
    {
        private ClanInfo info;
        private IList<ClanTerrainModifierInfo> terrainModifiers = new List<ClanTerrainModifierInfo>();

        public string DisplayName { get => this.info.DisplayName; set => this.info.DisplayName = value; }

        public string ShortName { get => this.info.ShortName; }

        public Player Player { get => Game.Current.Players.Find(p => p.Clan == this); }

        public static Clan Create(ClanInfo info)
        {
            return new Clan(info);
        }

        private Clan(ClanInfo info)
        {
            this.info = info;
        }

        public ClanInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindClanInfo(this.ShortName);
                return this.info;
            }
        }

        public IList<ClanTerrainModifierInfo> GetTerrainModifiers()
        {
            return new List<ClanTerrainModifierInfo>(this.terrainModifiers);
        }

        public void AddTerrainModifier(ClanTerrainModifierInfo modifier)
        {
            this.terrainModifiers.Add(modifier);
        }

        public int GetTerrainModifier(string terrainId)
        {
            if (string.IsNullOrWhiteSpace(terrainId))
            {
                throw new ArgumentException("message", nameof(terrainId));
            }

            IList<ClanTerrainModifierInfo> modifiers = ModFactory.FindClanTerrainMappingInfos(this.ShortName);
            foreach (ClanTerrainModifierInfo value in modifiers)
            {
                if (value.TerrainName == terrainId)
                {
                    return value.Modifier;
                }
            }

            return 0;
        }

        public int GetTerrainModifier(Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            return GetTerrainModifier(tile.Terrain.ShortName);
        }

        public override string ToString()
        {
            return this.ShortName;
        }

        public override bool Equals(object obj)
        {
            Clan other = obj as Clan;

            if (other == null)
            {
                return false;
            }

            return other.ShortName == this.ShortName;
        }

        public override int GetHashCode()
        {
            return this.ShortName.GetHashCode();
        }
    }
}
