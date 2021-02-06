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

        public void TakeItems(Hero hero, List<IItem> items)
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

        public void DropItems(Hero hero, List<IItem> items)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                hero.Drop(item);
            }
        }
    }
}
