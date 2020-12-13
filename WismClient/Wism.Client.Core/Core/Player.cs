using System;
using System.Collections.Generic;
using Wism.Client.Agent.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class Player
    {
        private const int StartingGold = 432;

        private List<Army> myArmies = new List<Army>();
        private List<City> myCities = new List<City>();

        public Clan Clan { get; set; }

        public int Gold { get; set; }

        private Player()
        {
        }

        public static Player Create(Clan clan)
        {
            if (clan is null)
            {
                throw new System.ArgumentNullException(nameof(clan));
            }

            Player player = new Player()
            {
                Clan = clan,
                Gold = StartingGold
            };

            return player;
        }

        public List<Army> GetArmies()
        {
            return new List<Army>(this.myArmies);
        }

        public List<City> GetCities()
        {
            return new List<City>(this.myCities);
        }

        public int GetIncome()
        {
            int income = 0;

            foreach (City city in myCities)
            {
                income += city.Income;
            }

            return income;
        }

        public int GetUpkeep()
        {
            int upkeep = 0;

            foreach (Army army in myArmies)
            {
                upkeep += army.Upkeep;
            }

            return upkeep;
        }

        public Hero HireHero(Tile tile)
        {
            return (Hero)ConscriptArmy(ArmyInfo.GetHeroInfo(), tile);
        }

        public Army ConscriptArmy(ArmyInfo armyInfo, Tile tile)
        {
            if (armyInfo == null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (!CanDeploy(armyInfo, tile))
                throw new ArgumentException(
                    String.Format("Army type '{0}' cannot be deployed to '{1}'.", armyInfo.DisplayName, tile.Terrain.DisplayName));

            Army newArmy = ArmyFactory.CreateArmy(this, armyInfo);
            var newArmies = new List<Army>() { newArmy };

            DeployArmies(tile, newArmies);

            return newArmy;
        }

        /// <summary>
        /// End the players turn.
        /// </summary>
        /// <remarks>
        /// Resets moves, triggers production, and allows for other clans 
        /// to complete their turns.
        /// </remarks>
        internal void EndTurn()
        {
            if (Game.Current.GetCurrentPlayer() != this)
            {
                throw new InvalidOperationException("Cannot end turn; it's not my turn!");
            }

            DoTheBooks();
            ResetArmies();
        }

        /// <summary>
        /// Process city income and army upkeep for the turn.
        /// </summary>
        private void DoTheBooks()
        {
            Gold += GetIncome() - GetUpkeep();
            if (Gold < 0)
            {               
                Gold = 0;
            }
        }

        /// <summary>
        /// Reset armies to their start-of-turn state.
        /// </summary>
        private void ResetArmies()
        {
            foreach (Army army in GetArmies())
            {
                if (army.IsDead)
                {
                    throw new InvalidOperationException("Player cannot reset a dead army.");
                }

                army.Reset();
                army.MovesRemaining = army.Info.Moves;
            }
        }

        internal void KillArmy(Army army)
        {
            // Remove from the world
            var armies = new List<Army>() { army };
            var tile = army.Tile;
            if (tile.ContainsArmies(armies))
            {
                tile.RemoveArmies(armies);
            }                 
            else
            {
                // It was an attacking army
                tile.RemoveVisitingArmies(armies);
                Game.Current.RemoveSelectedArmies(armies);
            }

            // Remove from player armies for tracking
            myArmies.Remove(army);
        }

        private void DeployArmies(Tile tile, List<Army> newArmies)
        {
            // Deploy to the world
            tile.AddArmies(newArmies);

            // Add to player armies for tracking            
            this.myArmies.AddRange(newArmies);
        }

        private bool CanDeploy(ArmyInfo armyInfo, Tile tile)
        {
            if (armyInfo == null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            Terrain terrain = tile.Terrain;
            return ((terrain.CanTraverse(armyInfo.CanWalk, armyInfo.CanFloat, armyInfo.CanFly)) &&
                    (!tile.HasArmies() || (tile.Armies.Count < Army.MaxUnits)));
        }

        public override string ToString()
        {
            return this.Clan.ToString();
        }

        /// <summary>
        /// Stake a claims for a city.
        /// </summary>
        /// <param name="city">City to claim</param>
        public void ClaimCity(City city)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            ClaimCity(city, city.GetTiles());            
        }

        /// <summary>
        /// Stake a claim for a city; Internal-only used by MapBuilder
        /// </summary>
        /// <param name="city">City to claim</param>
        /// <param name="tiles">Tiles for the city</param>
        internal void ClaimCity(City city, Tile[] tiles)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            if (tiles is null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            // Are we claiming from another clan?
            if (city.Clan != Clan && city.Clan != null)
            {                
                PillageGoldFromClan(city.Clan);
                city.Clan.Player.RemoveCity(city);
            }

            city.Claim(Clan, tiles);                        

            // Add city to Player for tracking
            this.myCities.Add(city);
        }

        private void RemoveCity(City city)
        {
            this.myCities.Remove(city);
        }

        /// <summary>
        /// Pillage gold from the clan's city.
        /// </summary>
        /// <param name="clan">Clan to pillage from.</param>
        /// <remarks>
        ///  An Empire's treasury is considered to be distributed equally amongst all of
        ///  its cities. If a city is captured, the Empire loses a proportional amount of
        ///  its total treasury. Half of this money goes to the Empire that captured the
        ///  city; the other half is considered to be liberated by the troops that did
        ///  the pillaging!
        /// </remarks>
        private void PillageGoldFromClan(Clan clan)
        {
            // Find matching player
            Player playerToPillage = Game.Current.Players.Find(p => p.Clan == clan);
            if (playerToPillage == null)
            {
                return;
            }

            // Assume player-to-pillage's cities will be > 0 as we haven't claimed it yet

            int cityCoffers = playerToPillage.Gold / playerToPillage.GetCities().Count;
            Gold += (playerToPillage.Gold / playerToPillage.GetCities().Count) / 2;
            
            playerToPillage.Gold -= cityCoffers;
            if (playerToPillage.Gold < 0)
            {
                playerToPillage.Gold = 0;
            }
        }

        public void RazeCity(City city)
        {
            if (city.Clan != Clan)
            {
                throw new ArgumentException($"Cannot raze a city not owned by the player. Player: {this}, City: {city}");
            }

            city.Raze();

            // Remove city from Player tracking
            this.myCities.Remove(city);
        }
    }
}