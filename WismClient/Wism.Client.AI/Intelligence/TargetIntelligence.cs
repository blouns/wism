using System;
using Wism.Client.Core;

namespace Wism.Client.AI.Task
{
    /// <summary>
    ///     Detect MapObjects to action on.
    /// </summary>
    public class TargetIntelligence
    {
        public TargetIntelligence(World world)
        {
            this.World = world ?? throw new ArgumentNullException(nameof(world));
        }

        public World World { get; }

        /// <summary>
        ///     Find objects on map relavant to player and stuff them in the bag.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <returns>Bag of objects</returns>
        public TargetPortfolio FindTargetObjects(Player player)
        {
            var bag = new TargetPortfolio();

            this.FindOpposingArmies(player, bag);
            this.FindOpposingCities(player, bag);
            this.FindNeutralCities(player, bag);
            this.FindLooseItems(player, bag);
            this.FindUnsearchedLocations(player, bag);

            return bag;
        }

        /// <summary>
        ///     Find locations that have not been searched.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the locations</param>
        private void FindUnsearchedLocations(Player player, TargetPortfolio bag)
        {
            // TODO: Filter for 'near-me'
            var locations = this.World.GetLocations().FindAll(l => !l.Searched);

            bag.UnsearchedLocations = locations;
        }

        /// <summary>
        ///     Find loose items on the ground.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the items</param>
        private void FindLooseItems(Player player, TargetPortfolio bag)
        {
            // TODO: Filter for 'near-me'
            var looseItems = this.World.GetLooseItems();

            bag.LooseItems = looseItems;
        }

        /// <summary>
        ///     Find cities not owned by the player.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the cities</param>
        private void FindOpposingCities(Player player, TargetPortfolio bag)
        {
            // TODO: Filter to cities 'near-me'
            var cities = this.World.GetCities()
                .FindAll(city => city.Player != player && city.Clan.ShortName != "Neutral");

            bag.OpposingCities = cities;
        }

        /// <summary>
        ///     Find neutral cities.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the cities in</param>
        private void FindNeutralCities(Player player, TargetPortfolio bag)
        {
            // TODO: Filter to cities 'near-me'
            var cities = this.World.GetCities().FindAll(city => city.Clan.ShortName == "Neutral");

            bag.NeutralCities = cities;
        }

        /// <summary>
        ///     Find opposing armies.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the armies</param>
        private void FindOpposingArmies(Player player, TargetPortfolio bag)
        {
            // TODO: Filter for armies near 'my assets'
            var armies = Game.Current.GetAllArmies()
                .FindAll(a => a.Player != player);

            bag.OpposingArmies = armies;
        }
    }
}