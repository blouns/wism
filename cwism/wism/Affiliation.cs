using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{

    public class Affiliation : ICustomizable
    {
        private AffiliationInfo info;
        private IList<AffiliationTerrainModifierInfo> terrainModifiers = new List<AffiliationTerrainModifierInfo>();

        private bool isHuman = false;

        public string DisplayName { get => info.DisplayName; set => info.DisplayName = value; }

        public bool IsHuman { get => isHuman; set => isHuman = value; }

        public string ID { get => info.ID; }

        public static Affiliation Create(AffiliationInfo info)
        {
            return new Affiliation(info);
        }

        private Affiliation(AffiliationInfo info)
        {
            this.info = info;
        }

        public AffiliationInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindAffiliationInfo(this.ID);
                return info;
            }
        }

        public IList<AffiliationTerrainModifierInfo> GetTerrainModifiers()
        {
            return new List<AffiliationTerrainModifierInfo>(this.terrainModifiers);
        }

        public void AddTerrainModifier(AffiliationTerrainModifierInfo modifier)
        {
            this.terrainModifiers.Add(modifier);
        }

        public int GetTerrainModifier(string terrainId)
        {
            if (string.IsNullOrWhiteSpace(terrainId))
            {
                throw new ArgumentException("message", nameof(terrainId));
            }

            IList<AffiliationTerrainModifierInfo> modifiers = ModFactory.FindAffiliationTerrainMappingInfos(this.ID);
            foreach (AffiliationTerrainModifierInfo value in modifiers)
            {
                if (value.TerrainID == terrainId)
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

            return GetTerrainModifier(tile.Terrain.ID);
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", 
                this.DisplayName, 
                isHuman ? "Human" : "CPU");
        }
    }
}
