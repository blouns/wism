using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Controllers
{
    public class CityController
    {
        private readonly ILogger logger;

        public CityController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        /// <summary>
        ///     Renew production projects
        /// </summary>
        /// <param name="player">Player to renew production</param>
        /// <param name="productionToRenew">Armies that have been renewed for production</param>
        /// <returns>Success if all production renewed; otherwise False</returns>
        public ActionState RenewProduction(Player player, List<ArmyInTraining> productionToRenew)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (productionToRenew is null)
            {
                throw new ArgumentNullException(nameof(productionToRenew));
            }

            var state = ActionState.Failed;
            var success = true;

            foreach (var armyToRenew in productionToRenew)
            {
                if (armyToRenew.DestinationCity != null)
                {
                    success &= this.TryStartingProductionToDestination(
                        armyToRenew.ProductionCity,
                        armyToRenew.ArmyInfo,
                        armyToRenew.DestinationCity);
                }
                else
                {
                    success &= this.TryStartingProduction(
                        armyToRenew.ProductionCity,
                        armyToRenew.ArmyInfo);
                }
            }

            if (success)
            {
                state = ActionState.Succeeded;
            }

            return state;
        }

        /// <summary>
        ///     Renew all completed production projects.
        /// </summary>
        /// <param name="player">Player to renew production for</param>
        /// <param name="armiesProduced">Armies produced or empty</param>
        /// <param name="armiesDelivered">Armies delivered or empty</param>
        /// <returns>True if any armies are returned (produced or delivered); otherwise False</returns>
        public bool TryGetProducedArmies(Player player, out List<ArmyInTraining> armiesProduced,
            out List<ArmyInTraining> armiesDelivered)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            armiesProduced = new List<ArmyInTraining>();
            if (player.AnyArmiesProduced())
            {
                armiesProduced.AddRange(player.GetProducedArmies());
            }

            armiesDelivered = new List<ArmyInTraining>();
            if (player.AnyArmiesDelivered())
            {
                armiesDelivered.AddRange(player.GetDeliveredArmies());
            }

            return armiesDelivered.Count > 0 || armiesProduced.Count > 0;
        }

        /// <summary>
        ///     Claim a city for a given player.
        /// </summary>
        /// <param name="city">City to claim</param>
        /// <param name="player">Player who will stake the claim</param>
        public void ClaimCity(City city, Player player)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            player.ClaimCity(city);
        }

        /// <summary>
        ///     Gets the cost to build the cities defenses.
        /// </summary>
        /// <param name="city">City to build</param>
        /// <returns>Cost in gp</returns>
        public int GetBuildCost(City city)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            return city.GetCostToBuild();
        }

        /// <summary>
        ///     Try to build the cities defenses by one
        /// </summary>
        /// <param name="city">City to build</param>
        /// <returns>True if improved; otherwise False</returns>
        public bool TryBuildDefense(City city)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            return city.TryBuild();
        }

        /// <summary>
        ///     Destroy a city forever.
        /// </summary>
        /// <param name="city">City to raze</param>
        public void RazeCity(City city, Player player)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            player.RazeCity(city);
        }

        /// <summary>
        ///     Start production on an army.
        /// </summary>
        /// <param name="city">City to produce in</param>
        /// <param name="armyInfo">Army kind to produce</param>
        /// <returns>True if production started; otherwise, false (not enough money)</returns>
        public bool TryStartingProduction(City city, ArmyInfo armyInfo)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            return city.Barracks.StartProduction(armyInfo);
        }

        /// <summary>
        ///     Start production on an army that will be delivered to the destination city.
        /// </summary>
        /// <param name="city">City to produce from</param>
        /// <param name="armyInfo">Army kind to produce</param>
        /// <param name="destinationCity">City to deliver to</param>
        /// <returns>True if production started; otherwise, false (not enough money)</returns>
        public bool TryStartingProductionToDestination(City city, ArmyInfo armyInfo, City destinationCity)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (destinationCity is null)
            {
                throw new ArgumentNullException(nameof(destinationCity));
            }

            return city.Barracks.StartProduction(armyInfo, destinationCity);
        }

        /// <summary>
        ///     Cancel production.
        /// </summary>
        /// <param name="city">City to stop production</param>
        public void StopProduction(City city)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            city.Barracks.StopProduction();
        }
    }
}