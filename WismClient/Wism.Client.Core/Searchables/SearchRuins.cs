using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Searchables
{
    public class SearchRuins : ISearchable
    {
        private const float OddsToDefeatMonster = 0.9f;

        private SearchRuins()
        {
        }

        public static SearchRuins Instance { get; } = new SearchRuins();

        public bool CanSearchKind(string kind)
        {
            return kind == "Ruins" || kind == "Tomb";
        }

        public bool Search(List<Army> armies, Location location, out object result)
        {
            result = null;
            var hero = armies.Find(a =>
                a is Hero &&
                a.Tile == location.Tile &&
                a.MovesRemaining > 0);

            if (hero == null)
            {
                return false;
            }

            if (!location.Searched)
            {
                if (location.HasMonster() &&
                    !this.DefeatedMonster())
                {
                    // Hero was slain!
                    hero.Player.KillArmy(hero);
                    return false;
                }

                if (location.HasBoon())
                {
                    result = location.Boon.Redeem(location.Tile);
                }

                hero.MovesRemaining = 0;
                location.Searched = true;
            }

            return location.Searched;
        }

        /// <summary>
        ///     Fights the monster inhabiting the location
        /// </summary>
        /// <returns>True if monster is defeated; otherwise False</returns>
        private bool DefeatedMonster()
        {
            return Game.Current.Random.Next(0, 10) / 10 < OddsToDefeatMonster;
        }
    }
}