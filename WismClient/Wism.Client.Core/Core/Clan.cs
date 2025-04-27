using System;
using System.Collections.Generic;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core
{
    public class Clan
    {
        private readonly IList<ClanTerrainModifierInfo> terrainModifiers = new List<ClanTerrainModifierInfo>();
        private ClanInfo info;

        private Clan(ClanInfo info)
        {
            this.info = info;
        }

        public string DisplayName
        {
            get => this.info.DisplayName;
            set => this.info.DisplayName = value;
        }

        public string ShortName => this.info.ShortName;

        public Player Player => Game.Current.Players.Find(p => p.Clan == this);

        public ClanInfo Info
        {
            get
            {
                if (this.info == null)
                {
                    this.info = MapBuilder.FindClanInfo(this.ShortName);
                }

                return this.info;
            }
        }

        public static Clan Create(ClanInfo info)
        {
            return new Clan(info);
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

            var modifiers = ModFactory.FindClanTerrainMappingInfos(this.ShortName);
            foreach (var value in modifiers)
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

            return this.GetTerrainModifier(tile.Terrain.ShortName);
        }

        public override string ToString()
        {
            return this.ShortName;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Clan;

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