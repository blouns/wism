using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.MapObjects;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Heros
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
        public DropItemsCommand(HeroController heroController, MapObjects.Hero hero, List<Artifact> items)
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
        public DropItemsCommand(HeroController heroController, MapObjects.Hero hero)
            : this(heroController, hero, hero.Items)
        {
        }

        public MapObjects.Hero Hero { get; set; }
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

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var hero = this.Hero;
            var tile = hero?.Tile;

            return new CommandExecutedEvent
            {
                CommandType = nameof(DropItemsCommand),
                ActorId = hero?.ShortName ?? "UnknownHero",
                TargetId = tile?.ToString(),
                TargetPosition = tile != null
                    ? new PositionDto { X = tile.X, Y = tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Hero", hero?.ShortName ?? "Unknown" },
                    { "Owner", hero?.Player?.Clan?.ShortName ?? "Unknown" },
                    { "Tile", tile?.ToString() ?? "Unknown" },
                    { "ItemCount", Items?.Count ?? 0 },
                    { "ItemNames", string.Join(", ", Items?.Select(i => i.ShortName) ?? Enumerable.Empty<string>()) }
                }
            };
        }
    }
}