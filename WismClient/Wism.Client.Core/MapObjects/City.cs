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

            var tiles = GetTiles();
            for (int i = 0; i < 4; i++)
            {
                if (tiles[i].HasArmies())
                {
                    armies.AddRange(tiles[i].Armies);
                }
            }

            return armies;
        }

        /// <summary>
        /// Gets the tiles for the city (4x4)
        /// </summary>
        /// <returns>Array contain four tiles</returns>
        public Tile[] GetTiles()
        {
            if (Tile == null)
            {
                throw new InvalidOperationException("Cannot get tiles as the Tile was null.");
            }

            var nineGrid = Tile.GetNineGrid();

            return new Tile[4]
            {
                nineGrid[1, 1],
                nineGrid[1, 2],
                nineGrid[2, 1],
                nineGrid[2, 2]
            };

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
            var quadrants = GetTiles();
            for (int i = 0; i < 4; i++)
            {
                quadrants[i].RazeInternal();                
            }

            // TODO: Reset production
        }

        /// <summary>
        /// Stake a claim for the given clan
        /// </summary>
        /// <param name="clan">Clan to stake claim</param>
        public void Claim(Clan clan)
        {
            Claim(clan, GetTiles());
        }

        internal void Claim(Clan clan, Tile[] tiles)
        {
            if (clan is null)
            {
                throw new ArgumentNullException(nameof(clan));
            }

            if (tiles is null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }
            // Ensure all armies are friendly in the city
            var cityArmies = MusterArmies();
            if (!cityArmies.TrueForAll(a => a.Clan == clan))
            {
                throw new ArgumentException("Clan cannot claim a city when there are armies of another clan present.");
            }

            for (int i = 0; i < 4; i++)
            {
                if (tiles[i].City == null)
                {
                    throw new InvalidOperationException("Not able to claim as there is no city on this tile.");
                }
                tiles[i].City.Clan = clan;
            }

            // TODO: Reset production
        }

        public override string ToString()
        {
            return this.ShortName;
        }
    }
}
