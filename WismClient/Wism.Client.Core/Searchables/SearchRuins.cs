using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;

namespace Wism.Client.MapObjects
{
    public class SearchRuins : ISearchable
    {
        private const float oddsToDefeatMonster = 0.9f;
        private static readonly SearchRuins instance = new SearchRuins();

        public static SearchRuins Instance => instance;

        private SearchRuins()
        {
        }

        public bool CanSearchKind(string kind)
        {
            return kind == "Ruins" || kind == "Tomb";
        }

        public bool Search(List<Army> armies, Location location, out object result)
        {
            result = null;
            Army hero = armies.Find(a =>
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
                   !DefeatedMonster())
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
        /// Fights the monster inhabiting the location
        /// </summary>
        /// <returns>True if monster is defeated; otherwise False</returns>
        private bool DefeatedMonster()
        {
            return (Game.Current.Random.Next(0, 10) / 10) < oddsToDefeatMonster;
        }
    }
}
