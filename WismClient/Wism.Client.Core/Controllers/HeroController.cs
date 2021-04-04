using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Controllers
{
    public class HeroController
    {
        private readonly ILogger logger;

        public HeroController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        public Hero HireHero(Player player, Tile tile)
        {
            return player.HireHero(tile);
        }

        public void TakeItems(Hero hero, List<Artifact> items)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            hero.Take(items);
        }

        public void DropItems(Hero hero, List<Artifact> items)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in new List<Artifact>(items))
            {
                hero.Drop(item);
            }
        }
    }
}
