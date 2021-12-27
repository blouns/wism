using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class City : MapObject, IBuildable
    {
        public const int MaxDefense = 10; 

        private CityInfo info;

        public int Defense { get; set; }

        public Clan Clan { get; private set; }

        public int Income { get => Info.Income;  }        

        public override string ShortName { get => Info.ShortName; }

        public Barracks Barracks { get; set; }

        public CityInfo Info
        {
            get
            {
                if (this.info == null)
                {
                    this.info = MapBuilder.FindCityInfo(this.ShortName);
                }

                return info;
            }
        }

        private City(CityInfo info)
        {
            this.info = info ?? throw new System.ArgumentNullException(nameof(info));
            this.Defense = info.Defense;
            this.DisplayName = info.DisplayName;
            this.Barracks = new Barracks(this, info.ProductionInfos);
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
                nineGrid[1, 0],
                nineGrid[1, 1],
                nineGrid[2, 0],
                nineGrid[2, 1]
            };

        }    

        public bool TryBuild()
        {
            if (Defense == MaxDefense)
            {
                return false;
            }

            int cost = GetCostToBuild();
            if (Player.Gold >= cost)
            {
                Player.Gold -= cost;
                this.Defense++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetCostToBuild()
        {
            int cost;
            switch (this.Defense)
            {
                case 0:
                case 1:
                case 2:
                    cost = 50;
                    break;
                case 3:
                    cost = 75;
                    break;
                case 4:
                    cost = 100;
                    break;
                case 5:
                    cost = 150;
                    break;
                case 6:
                    cost = 175;
                    break;
                case 7:
                    cost = 350;
                    break;
                case 8:
                    cost = 500;
                    break;
                case 9:
                    cost = 800;
                    break;
                default:
                    cost = int.MaxValue;
                    break;
            }

            return cost;
        }

        /// <summary>
        /// Reduces the city to ruins! This is irreparable and causes a reputation hit.
        /// </summary>
        public void Raze()
        {            
            var tiles = GetTiles();
            for (int i = 0; i < 4; i++)
            {
                tiles[i].RazeInternal();                
            }

            // Reset production
            this.Barracks.Reset();
        }

        /// <summary>
        /// Stake a claim for the given player
        /// </summary>
        /// <param name="player">Player to stake claim</param>
        public void Claim(Player player)
        {
            Claim(player, Tile);
        }

        internal void Claim(Player player, Tile tile)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            // Ensure all armies are friendly in the city
            var cityArmies = MusterArmies();
            if (!cityArmies.TrueForAll(a => a.Clan == player.Clan))
            {
                throw new ArgumentException("Clan cannot claim a city when there are armies of another clan present.");
            }

            // Claim the city
            this.Player = player;
            this.Clan = player.Clan;
            this.Tile = tile;
            var tiles = GetTiles();
            for (int i = 0; i < 4; i++)
            {
                if (tiles[i].City == null)
                {
                    throw new InvalidOperationException("Not able to claim as there is no city on this tile.");
                }
                tiles[i].City.Clan = player.Clan;
            }

            // Reset production
            this.Barracks.Reset();
            CancelIncomingProduction();
        }

        private void CancelIncomingProduction()
        {
            if (this.Player == null)
            {
                // Neutral city
                return;
            }    

            var cities = this.Player.GetCities();
            foreach (City otherCity in cities)
            {
                otherCity.Barracks.CancelDelivery(this);                
            }
        }

        /// <summary>
        /// Start producing an army in the barracks.
        /// </summary>
        /// <param name="armyInfo"></param>
        /// <param name="destinationCity"></param>
        /// <returns></returns>
        internal bool ProduceArmy(ArmyInfo armyInfo, City destinationCity = null)
        {
            return this.Barracks.StartProduction(armyInfo, destinationCity);
        }

        public override string ToString()
        {
            return this.ShortName;
        }

        internal bool CanTraverse(Clan clan)
        {            
            if (clan is null)
            {
                throw new ArgumentNullException(nameof(clan));
            }

            return this.Clan == clan;
        }

        internal bool CanTraverse(Army army)
        {
            if (army is null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            return this.Clan == army.Clan;
        }

        public override bool Equals(object obj)
        {
            City other = (City)obj;
            return
                (ShortName == other.ShortName) &&
                (Tile == other.Tile);
        }

        public override int GetHashCode()
        {
            return $"{ShortName}{Tile}".GetHashCode();
        }
    }
}
