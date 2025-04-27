using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core.Heros
{
    /// <summary>
    ///     Default hero recruitment strategy based on number of current heros
    ///     and how long it's been since the last hero was hired. The first hero
    ///     is free!
    /// </summary>
    public class DefaultRecruitHeroStrategy : IRecruitHeroStrategy
    {
        private const double BringAlliesChance = 0.5;
        private const int MinAllies = 1;
        private const int MaxAllies = 3;
        private int heroNameIndex;

        private IList<string> heroNames;

        /// <summary>
        ///     Gets the hero's allies if they come with any.
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>List of new army kinds or an empty list</returns>
        /// <remarks>
        ///     Not all heros will come with allies. This strategy will select
        ///     a special army kind and random number of allies to join the
        ///     hero.
        /// </remarks>
        public List<ArmyInfo> GetAllies(Player player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var allies = new List<ArmyInfo>();

            if (player.Turn == 1)
            {
                // Don't start the game with allies
                return allies;
            }

            var specialArmyKinds = ModFactory.FindSpecialArmyInfos();
            if (specialArmyKinds.Count > 0)
            {
                // Calculate the chance the hero will bring allies
                var chance = Game.Current.Random.NextDouble();
                if (chance > BringAlliesChance)
                {
                    // Choose a random special army kind
                    var specialIndex = Game.Current.Random.Next(specialArmyKinds.Count);

                    // Add up to MaxAllies of the chosen armies
                    var allyCount = Game.Current.Random.Next(MinAllies, MaxAllies);

                    for (var i = 0; i < allyCount; i++)
                    {
                        allies.Add(specialArmyKinds[specialIndex]);
                    }
                }
            }

            return allies;
        }

        /// <summary>
        ///     Gets a random hero name.
        /// </summary>
        /// <returns>Hero name</returns>
        public string GetHeroName()
        {
            if (this.heroNames == null)
            {
                var path = ModFactory.ModPath + "\\" + ModFactory.HeroPath;
                this.heroNames = RandomizeList(
                    Game.Current.Random,
                    ModFactory.LoadHeroNames(path));
            }

            if (this.heroNames.Count == 0)
            {
                return "Branally";
            }

            var name = this.heroNames[this.heroNameIndex];
            this.heroNameIndex = (this.heroNameIndex + 1) % this.heroNames.Count;

            return name;
        }

        /// <summary>
        ///     Gets a random hero price between <c>Hero.MinGoldToHire</c>
        ///     and <c>Hero.MaxGoldToHire</c>.
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>Hero's price</returns>
        public int GetHeroPrice(Player player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            int goldToHire;
            if (player.Turn == 1)
            {
                // First hero is free
                goldToHire = 0;
            }
            else if (player.GetCities().Count == 0)
            {
                // Must have a city
                goldToHire = int.MaxValue;
            }
            else
            {
                // Random price
                goldToHire = Game.Current.Random.Next(Hero.MinGoldToHire, Hero.MaxGoldToHire);
            }

            return goldToHire;
        }

        /// <summary>
        ///     Returns a random city owned by the player.
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>City for the new hero</returns>
        public City GetTargetCity(Player player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var cities = player.GetCities();
            var randomCityIndex = Game.Current.Random.Next(cities.Count);

            return cities[randomCityIndex];
        }

        /// <summary>
        ///     Checks if a new hero is available for the given player.
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>True if a hero is available; otherwise, False</returns>
        /// <remarks>
        ///     Strategy is based on two primary factors and the first turn always
        ///     gets a free hero. No heros are available without at least one city.
        ///     Factors:
        ///     1. Time since last hero (more time is more likely)
        ///     2. Number of heros (less heros is more likely)
        /// </remarks>
        public bool IsHeroAvailable(Player player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            // Must have at least one city to attract a hero
            if (player.GetCities().Count == 0)
            {
                return false;
            }

            // First turn always gets a new hero
            if (player.Turn == 1)
            {
                return true;
            }

            // Chance goes down based on number of current heros
            var heros = player.GetArmies().FindAll(a => a is Hero);
            var heroCountChance = 1 - Math.Log10(heros.Count + 1);
            if (heroCountChance < 0)
            {
                heroCountChance = 0;
            }

            // Chance goes up based on number of turns without a new hero
            var turnsSinceLastHero = player.Turn - player.LastHeroTurn;
            var turnsSinceLastHeroChance = Math.Log10(turnsSinceLastHero);
            if (turnsSinceLastHeroChance > 1)
            {
                turnsSinceLastHeroChance = 1;
            }

            // Calculate if hero is available
            var chance = Game.Current.Random.NextDouble();
            var isHeroForHire = chance < heroCountChance * turnsSinceLastHeroChance;

            return isHeroForHire;
        }

        private static IList<T> RandomizeList<T>(Random r, IEnumerable<T> source)
        {
            var list = new List<T>();
            foreach (var item in source)
            {
                var i = r.Next(list.Count + 1);
                if (i == list.Count)
                {
                    list.Add(item);
                }
                else
                {
                    var temp = list[i];
                    list[i] = item;
                    list.Add(temp);
                }
            }

            return list;
        }
    }
}