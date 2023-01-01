using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Heros;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class Player
    {
        // TODO: Starting gold varies by clan
        private const int StartingGold = 100;

        private List<Army> myArmies = new List<Army>();
        private List<Hero> myHeros = new List<Hero>();
        private List<City> myCities = new List<City>();

        private int lastHeroTurn;
        private List<ArmyInTraining> armiesProduced = new List<ArmyInTraining>();
        private List<ArmyInTraining> armiesDelivered = new List<ArmyInTraining>();

        private readonly IRecruitHeroStrategy recruitHeroStrategy;
        private readonly IDeploymentStrategy deploymentStrategy = new DefaultDeploymentStrategy();

        public Clan Clan { get; set; }

        public int Gold { get; set; }

        public int Turn { get; internal set; }

        public bool IsDead { get; set; }

        public bool IsHuman { get; set; }

        public City Capitol { get; set; }

        public int LastHeroTurn { get => this.lastHeroTurn; internal set => this.lastHeroTurn = value; }

        public IRecruitHeroStrategy RecruitHeroStrategy => this.recruitHeroStrategy;

        private Player(IRecruitHeroStrategy recruitingStrategy)
        {
            if (recruitingStrategy == null)
            {
                recruitingStrategy = new DefaultRecruitHeroStrategy();
            }

            this.recruitHeroStrategy = recruitingStrategy;
        }

        public static Player Create(Clan clan)
        {
            if (clan is null)
            {
                throw new System.ArgumentNullException(nameof(clan));
            }

            var heroPath = ModFactory.ModPath + "\\" + ModFactory.HeroPath;
            var recruitingStrategy = ModFactory.LoadRecruitHeroStrategy(heroPath);
            Player player = new Player(recruitingStrategy)
            {
                Clan = clan,
                Gold = StartingGold,
                Turn = 1
            };

            return player;
        }

        public static Player GetNeutralPlayer()
        {
            ClanInfo clanInfo = ClanInfo.GetClanInfo("Neutral");
            Clan clan = Clan.Create(clanInfo);
            return Player.Create(clan);
        }

        public List<Army> GetArmies()
        {
            return new List<Army>(this.myArmies);
        }

        public List<City> GetCities()
        {
            return new List<City>(this.myCities);
        }

        public List<Hero> GetHeros()
        {
            var herosAsArmyList = this.myArmies.FindAll(a => a is Hero);
            var heros = new List<Hero>();
            foreach (var hero in herosAsArmyList)
            {
                heros.Add((Hero)hero);
            }

            return heros;
        }

        public int GetIncome()
        {
            return this.myCities.Sum(city => city.Income);
        }

        public int GetUpkeep()
        {
            return this.myArmies.Sum(army => army.Upkeep);
        }

        /// <summary>
        /// Attempts to hire a new hero.
        /// </summary>
        /// <param name="tile">Tile to deploy the new hero to</param>
        /// <param name="price">Cost of the new hero in gp</param>
        /// <param name="displayName">Display name for the new hero</param>
        /// <param name="hero">Hero who was hired, if available</param>
        /// <returns>True if hero was hired; otherwise False</returns>
        public bool TryHireHero(Tile tile, int price, string displayName, out Hero hero)
        {
            hero = null;

            if (this.myHeros.Count >= Hero.MaxHeros)
            {
                return false;
            }

            if (this.Gold < price)
            {
                return false;
            }

            // Pay the hero and reset the price
            this.Gold -= price;

            // Get him a uniform!
            hero = (Hero)ConscriptArmy(ArmyInfo.GetHeroInfo(), tile);
            hero.DisplayName = displayName;

            this.myHeros.Add(hero);
            this.lastHeroTurn = this.Turn;

            return true;
        }

        /// <summary>
        /// Hires a new hero if there is sufficient gold
        /// </summary>
        /// <param name="tile">Tile to deploy the new hero to.</param>
        /// <param name="price">Cost of the new hero in gp.</param>
        /// <returns>New hero deployed to tile</returns>
        public Hero HireHero(Tile tile, int price = 0)
        {
            if (this.myHeros.Count >= Hero.MaxHeros)
            {
                throw new InvalidOperationException("Reached max hero limit.");
            }

            if (this.Gold < price)
            {
                throw new InvalidOperationException("Cannot afford a new hero!");
            }

            // Pay the hero and reset the price
            this.Gold -= price;

            // Get him a uniform!
            var hero = (Hero)ConscriptArmy(ArmyInfo.GetHeroInfo(), tile);

            // Get a random name
            hero.DisplayName = this.RecruitHeroStrategy.GetHeroName();

            this.myHeros.Add(hero);
            this.lastHeroTurn = this.Turn;

            return hero;
        }

        /// <summary>
        /// Side-load an army for loading only.
        /// </summary>
        /// <param name="army">Army to add</param>
        internal void AddArmy(Army army)
        {
            if (army is null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            this.myArmies.Add(army);
        }

        /// <summary>
        /// Side-load a hero for loading only.
        /// </summary>
        /// <param name="hero">Hero to add</param>
        /// <remarks>
        /// This is a tracking list only. All Heros should be added to both 
        /// <c>myArmies</c> and <c>myHeros</c>.
        /// </remarks>
        internal void AddHero(Hero hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            this.myHeros.Add(hero);
        }

        internal void AddCity(City city)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            if (this.myCities.Contains(city))
            {
                return;
            }

            this.myCities.Add(city);
        }

        /// <summary>
        /// Conscript an army from an army-in-training. Overrides defaults
        /// with city-specific modifiers from training.
        /// </summary>
        /// <param name="ait">Army-in-training</param>
        /// <param name="tile">Destination tile</param>
        /// <returns>New army</returns>
        public Army ConscriptArmy(ArmyInTraining ait, Tile tile)
        {
            var army = ConscriptArmy(ait.ArmyInfo, tile);
            army.Upkeep = ait.Upkeep;
            army.Strength = ait.Strength;
            army.Moves = ait.Moves;
            army.MovesRemaining = army.Moves;
            army.DisplayName = ait.DisplayName;

            return army;
        }

        /// <summary>
        /// Conscript an army from an army-in-training.
        /// </summary>
        /// <param name="armyInfo">Army kind</param>
        /// <param name="tile">Destination tile</param>
        /// <returns>New army</returns>
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

            Army newArmy = ArmyFactory.CreateArmy(this, armyInfo);
            var newArmies = new List<Army>() { newArmy };
            var targetTile = this.deploymentStrategy.FindNextOpenTile(this, armyInfo, tile);
            DeployArmies(targetTile, newArmies);

            return newArmy;
        }

        /// <summary>
        /// Start production on a new army
        /// </summary>
        /// <param name="armyInfo">Army kind to produce</param>
        /// <param name="productionCity">City to produce it from</param>
        /// <param name="destinationCity">Desitination city for the army</param>
        /// <returns>True if there is sufficient funds to start producing; otherwise, false</returns>
        public bool ProduceArmy(ArmyInfo armyInfo, City productionCity, City destinationCity = null)
        {
            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (productionCity is null)
            {
                throw new ArgumentNullException(nameof(productionCity));
            }

            return productionCity.ProduceArmy(armyInfo, destinationCity);
        }

        /// <summary>
        /// End the player's turn.
        /// </summary>
        internal void EndTurn()
        {
            if (Game.Current.GetCurrentPlayer() != this)
            {
                throw new InvalidOperationException("Cannot end turn; it's not my turn!");
            }

            this.Turn++;
            ResetArmies();
            this.armiesProduced.Clear();
            this.armiesDelivered.Clear();
        }

        /// <summary>
        /// Start the player's turn
        /// </summary>
        internal void StartTurn()
        {
            if (Game.Current.GetCurrentPlayer() != this)
            {
                throw new InvalidOperationException("Cannot start turn; it's not my turn!");
            }

            if (this.IsDead)
            {
                return;
            }

            DoTheBooks();
            ProduceArmies();
            DeliverArmies();
        }

        internal void DeliverArmies()
        {
            foreach (var city in this.myCities)
            {
                if (city.Barracks.Deliver(out ArmyInTraining newArmy))
                {
                    this.armiesDelivered.Add(newArmy);
                }
            }
        }

        internal bool AnyArmiesDelivered()
        {
            return this.armiesDelivered.Count > 0;
        }

        internal void ClearDeliveredArmies()
        {
            this.armiesDelivered.Clear();
        }

        internal IEnumerable<ArmyInTraining> GetDeliveredArmies()
        {
            return this.armiesDelivered;
        }

        internal void ProduceArmies()
        {
            foreach (var city in this.myCities)
            {
                if (city.Barracks.Produce(out ArmyInTraining newArmy))
                {
                    this.armiesProduced.Add(newArmy);
                }
            }
        }

        internal bool AnyArmiesProduced()
        {
            return this.armiesProduced.Count > 0;
        }

        internal void ClearProducedArmies()
        {
            this.armiesProduced.Clear();
        }

        internal IEnumerable<ArmyInTraining> GetProducedArmies()
        {
            return this.armiesProduced;
        }

        /// <summary>
        /// Process city income and army upkeep for the turn.
        /// </summary>
        private void DoTheBooks()
        {
            this.Gold += GetIncome() - GetUpkeep();
            if (this.Gold < 0)
            {
                this.Gold = 0;
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
                Game.Current.RemoveSelectedArmies(armies);
            }

            // Remove from player armies for tracking
            this.myArmies.Remove(army);
            var hero = army as Hero;
            if (hero != null)
            {
                this.myHeros.Remove(hero);
            }
        }

        private void DeployArmies(Tile tile, List<Army> newArmies)
        {
            // Deploy to the world
            tile.AddArmies(newArmies);

            // Add to player armies for tracking            
            this.myArmies.AddRange(newArmies);
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

            ClaimCity(city, city.Tile);
        }

        /// <summary>
        /// Stake a claim for a city; Internal-only used by MapBuilder
        /// </summary>
        /// <param name="city">City to claim</param>
        /// <param name="tile">Upper-left tile for the city</param>
        internal void ClaimCity(City city, Tile tile)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            // Are we claiming from another clan?
            if (city.Clan != null &&
                city.Clan.ShortName != Player.GetNeutralPlayer().Clan.ShortName)
            {
                PillageGoldFromClan(city.Clan);
                city.Clan.Player.RemoveCity(city);
            }

            city.Claim(this, tile);

            // Add city to Player for tracking
            this.myCities.Add(city);
            if (this.Capitol == null)
            {
                this.Capitol = city;
            }
        }

        private void RemoveCity(City city)
        {
            this.myCities.Remove(city);

            // Move capitol if lost
            if (this.myCities.Count > 0)
            {
                this.Capitol = this.myCities[0];
            }
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
            int goldToPillage = (playerToPillage.Gold / playerToPillage.GetCities().Count) / 2;
            this.Gold += goldToPillage;

            playerToPillage.Gold -= cityCoffers;
            if (playerToPillage.Gold < 0)
            {
                playerToPillage.Gold = 0;
            }
        }

        public void RazeCity(City city)
        {
            if (city.Clan != this.Clan)
            {
                throw new ArgumentException($"Cannot raze a city not owned by the player. Player: {this}, City: {city}");
            }

            city.Raze();

            // Remove city from Player tracking
            this.myCities.Remove(city);
        }

        public override bool Equals(object obj)
        {
            Player other = obj as Player;

            if (other == null)
            {
                return false;
            }

            return other.Clan == this.Clan;
        }

        public override int GetHashCode()
        {
            return this.Clan.GetHashCode();
        }
    }
}