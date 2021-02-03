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

            if (!location.Searched &&
                armies.Any(a => a is Hero))
            {
                if (location.HasMonster() &&
                   !DefeatedMonster())
                {
                    Army hero = armies.Find(a => a is Hero);
                    hero.Player.KillArmy(hero);
                }

                if (location.HasBoon())
                {
                    result = location.Boon;
                }                

                location.Searched = true;
            }

            return result != null;
        }

        /// <summary>
        /// 
        /// </summary>
        private bool DefeatedMonster()
        {
            return (Game.Current.Random.Next(0, 10) / 10) < oddsToDefeatMonster;
        }
    }
}
