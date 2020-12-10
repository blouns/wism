using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{ 
    public class City : MapObject, IBuildable
    {
        public const int MaxDefense = 9; 

        private CityInfo info;

        public int Defense { get; set; }

        public Clan Clan { get; private set; }

        public override string DisplayName { get => Info.DisplayName; }

        public override string ShortName { get => Info.ShortName; }        

        public CityInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindCityInfo(this.ShortName);
                return info;
            }
        }

        private City(CityInfo info)
        {
            this.info = info ?? throw new System.ArgumentNullException(nameof(info));
            this.Defense = info.Defense;
        }

        public static City Create(CityInfo info)
        {
            return new City(info);
        }

        public City Clone()
        {
            return Create(this.Info);            
        }

        public List<Army> MusterArmies()
        {
            List<Army> armies = new List<Army>();

            var quadrants = GetTileQuadrants();
            for (int i = 0; i < 4; i++)
            {
                if (quadrants[i].Tile.HasArmies())
                {
                    armies.AddRange(quadrants[i].Tile.Armies);
                }
            }

            return armies;
        }

        public (Tile Tile, Quadrant Quadrant)[] GetTileQuadrants()
        {
            var quadrants = new (Tile, Quadrant)[4];

            // The MapObject's tile is top-left quadrant by convention
            quadrants[0] = (Tile, Quadrant.TopLeft);
            quadrants[1] = (World.Current.Map[Tile.X + 1, Tile.Y], Quadrant.TopRight);
            quadrants[2] = (World.Current.Map[Tile.X, Tile.Y - 1], Quadrant.BottomLeft);
            quadrants[3] = (World.Current.Map[Tile.X + 1, Tile.Y - 1], Quadrant.BottomRight);

            return quadrants;
        }

        public Quadrant GetQuadrant(Tile tile)
        {
            var tileQuadrants = GetTileQuadrants();

            for (int i = 0; i < 4; i++)
            {
                if (tile == tileQuadrants[i].Tile)
                {
                    return tileQuadrants[i].Quadrant;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tile));
        }

        public bool TryBuild()
        {
            if (Defense == MaxDefense)
            {
                return false;
            }

            // TODO: Costs money
            this.Defense++;

            return true;
        }

        /// <summary>
        /// Reduces the city to ruins! This is irreparable and causes a reputation hit.
        /// </summary>
        public void Raze()
        {            
            var quadrants = GetTileQuadrants();
            for (int i = 0; i < 4; i++)
            {
                quadrants[i].Tile.RazeInternal();                
            }

            // TODO: Reset production
        }

        /// <summary>
        /// Stake a claim for the given clan
        /// </summary>
        /// <param name="clan"></param>
        internal void Claim(Clan clan)
        {
            var quadrants = GetTileQuadrants();
            for (int i = 0; i < 4; i++)
            {
                quadrants[i].Tile.City.Clan = clan;
            }

            // TODO: Ensure all armies are friendly
            // TODO: Reset production
        }

        internal void Claim(Clan clan, Tile[] tile4x4)
        {           
            for (int i = 0; i < 4; i++)
            {
                if (tile4x4[i].City == null)
                {
                    throw new InvalidOperationException("Not able to claim as there is no city on this tile.");
                }
                tile4x4[i].City.Clan = clan;
            }

            // TODO: Ensure all armies are friendly
            // TODO: Reset production
        }
    }

    /// <summary>
    /// Helper enum to identify corner of the city (4x4)
    /// </summary>
    public enum Quadrant
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }
}
