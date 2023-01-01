using System;
using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class DropItemsCommand : Command
    {
        protected readonly HeroController heroController;

        /// <summary>
        ///     Drop items on current tile
        /// </summary>
        /// <param name="heroController">Hero controller</param>
        /// <param name="hero">Hero to drop items</param>
        /// <param name="items">Items to drop</param>
        public DropItemsCommand(HeroController heroController, Hero hero, List<Artifact> items)
        {
            this.heroController = heroController ?? throw new ArgumentNullException(nameof(heroController));
            this.Hero = hero ?? throw new ArgumentNullException(nameof(hero));
            this.Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <summary>
        ///     Drop all items on current tile
        /// </summary>
        /// <param name="heroController">Hero controller</param>
        /// <param name="hero">Hero to drop the items</param>
        public DropItemsCommand(HeroController heroController, Hero hero)
            : this(heroController, hero, hero.Items)
        {
        }

        public Hero Hero { get; set; }
        public List<Artifact> Items { get; }

        protected override ActionState ExecuteInternal()
        {
            this.heroController.DropItems(this.Hero, this.Items);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {this.Hero} dropping item(s) {this.Items}";
        }
    }
}