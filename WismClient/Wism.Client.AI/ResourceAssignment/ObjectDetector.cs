using System;
using System.Collections.Generic;
using Wism.Client.Core;

namespace Wism.Client.AI.ResourceAssignment
{
    /// <summary>
    /// Detect MapObjects to action on.
    /// </summary>
    public class ObjectDetector
    {
        public World World { get; }

        public ObjectDetector(World world)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        /// Find objects on map relavant to player and stuff them in the bag.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <returns>Bag of objects</returns>
        public TaskObjectBag FindTaskableObjects(Player player)
        {
            TaskObjectBag bag = new TaskObjectBag();

            FindOpposingArmies(player, bag);
            FindCities(player, bag);
            FindLooseItems(player, bag);
            FindUnsearchedLocations(player, bag);

            return bag;
        }

        /// <summary>
        /// Find locations that have not been searched.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the locations</param>
        private void FindUnsearchedLocations(Player player, TaskObjectBag bag)
        {
            // TODO: Filter for 'near-me'
            var locations = World.GetLocations().FindAll(l => !l.Searched);

            bag.UnsearchedLocations = locations.ConvertAll(a => new TaskableObject(a));
        }

        /// <summary>
        /// Find loose items on the ground.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the items</param>
        private void FindLooseItems(Player player, TaskObjectBag bag)
        {
            // TODO: Filter for 'near-me'
            var looseItems = World.GetLooseItems();

            bag.LooseItems = looseItems.ConvertAll(a => new TaskableObject(a));
        }

        /// <summary>
        /// Find cities.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the cities</param>
        private void FindCities(Player player, TaskObjectBag bag)
        {
            // TODO: Filter to cities 'near-me'
            var cities = World.GetCities();

            bag.AllCities = cities.ConvertAll(a => new TaskableObject(a));
        }

        /// <summary>
        /// Find opposing armies.
        /// </summary>
        /// <param name="player">Player to search on behalf of</param>
        /// <param name="bag">Bag to put the armies</param>
        private void FindOpposingArmies(Player player, TaskObjectBag bag)
        {
            // TODO: Filter for armies near 'my assets'
            var armies = Game.Current.GetAllArmies()
                .FindAll(a => a.Player != player);

            bag.OpposingArmies = armies.ConvertAll(a => new TaskableObject(a));
        }
    }
 }
