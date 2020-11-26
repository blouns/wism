using System;
using System.Collections.Generic;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class Clan
    {
        private ClanInfo info;
        private IList<ClanTerrainModifierInfo> terrainModifiers = new List<ClanTerrainModifierInfo>();

        private bool isHuman = false;

        public string DisplayName { get => info.DisplayName; set => info.DisplayName = value; }

        public bool IsHuman { get => isHuman; set => isHuman = value; }

        public string ShortName { get => info.ShortName; }

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
                return info;
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
            return String.Format("{0} ({1})",
                this.DisplayName,
                isHuman ? "Human" : "CPU");
        }
    }
}
