﻿using System;
using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class DropItemsCommand : Command
    {
        protected readonly HeroController heroController;
        public Hero Hero { get; set; }
        public List<IItem> Items { get; }

        /// <summary>
        /// Drop items on current tile
        /// </summary>
        /// <param name="heroController">Hero controller</param>
        /// <param name="hero">Hero to drop items</param>
        /// <param name="items">Items to drop</param>
        public DropItemsCommand(HeroController heroController, Hero hero, List<IItem> items)
        {
            this.heroController = heroController ?? throw new System.ArgumentNullException(nameof(heroController));
            Hero = hero ?? throw new System.ArgumentNullException(nameof(hero));
            Items = items ?? throw new System.ArgumentNullException(nameof(items));
        }

        /// <summary>
        /// Drop all items on current tile
        /// </summary>
        /// <param name="heroController">Hero controller</param>
        /// <param name="hero">Hero to drop the items</param>
        public DropItemsCommand(HeroController heroController, Hero hero)
            : this(heroController, hero, hero.Items)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            heroController.DropItems(Hero, Items);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {Hero} dropping item(s) {Items}";
        }
    }
}