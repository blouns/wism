using System;
using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class TakeItemsCommand : Command
    {
        protected readonly HeroController heroController;
        public Hero Hero { get; set; }
        public List<IItem> Items { get; }

        /// <summary>
        /// Take items
        /// </summary>
        /// <param name="heroController">Hero controller</param>
        /// <param name="hero">Hero to take items</param>
        /// <param name="items">Items to take</param>
        public TakeItemsCommand(HeroController heroController, Hero hero, List<IItem> items)
        {
            this.heroController = heroController ?? throw new System.ArgumentNullException(nameof(heroController));
            Hero = hero ?? throw new System.ArgumentNullException(nameof(hero));
            Items = items ?? throw new System.ArgumentNullException(nameof(items));
        }

        /// <summary>
        /// Take all items on current tile
        /// </summary>
        /// <param name="heroController">Hero controller</param>
        /// <param name="hero">Hero to take the items</param>
        public TakeItemsCommand(HeroController heroController, Hero hero)
            : this(heroController, hero, hero.Tile.Items)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            heroController.TakeItems(Hero, Items);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {Hero} taking item(s) {Items}";
        }
    }
}
