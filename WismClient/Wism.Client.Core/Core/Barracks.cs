﻿using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class Barracks
    {
        private const int DefaultTurnsToDeliver = 3;

        private readonly City city;
        private readonly Dictionary<ProductionInfo, int> productionInfoDictionary;

        public ArmyInTraining ArmyInTraining { get; set; }

        public Queue<ArmyInTraining> ArmiesToDeliver { get; set; }

        public Player Player { get => city.Player; }

        public Barracks(City city, ProductionInfo[] productionInfos)
        {
            this.city = city ?? throw new System.ArgumentNullException(nameof(city));

            if (productionInfos is null)
            {
                throw new ArgumentNullException(nameof(productionInfos));
            }

            this.productionInfoDictionary = new Dictionary<ProductionInfo, int>();
            for (int i = 0; i < productionInfos.Length; i++)
            {
                productionInfoDictionary.Add(productionInfos[i], 0);
            }
        }

        /// <summary>
        /// Start producing an army in the destination city
        /// </summary>
        /// <param name="armyInfo">Army kind to produce</param>
        /// <param name="destinationCity">City to deliver to (or null for current city)</param>
        /// <returns></returns>
        public bool StartProduction(ArmyInfo armyInfo, City destinationCity = null)
        {
            if (armyInfo is null)
            {
                throw new System.ArgumentNullException(nameof(armyInfo));
            }

            if (!CanProduce(armyInfo))
            {
                throw new ArgumentException($"This barracks cannot create {armyInfo.ShortName}");
            }

            if (!PlayerHasSufficientGold(armyInfo))
            {
                return false;
            }
            else
            {
                ChargePlayerForProductionCosts(armyInfo);
            }

            // Increment the number of armies of this type built
            var pi = FindProductionInfo(armyInfo);
            this.productionInfoDictionary[pi]++;

            ArmyInTraining = new ArmyInTraining()
            {
                ArmyInfo = armyInfo,
                DestinationCity = destinationCity,
                ProductionCity = this.city,
                TurnsToProduce = pi.TurnsToProduce,
                TurnsToDeliver = DefaultTurnsToDeliver,
                Upkeep = pi.Upkeep,
                MovesModifier = pi.MovesModifier,
                StrengthModifier = pi.StrengthModifier
            };
            

            return true;
        }

        /// <summary>
        /// Perform beginning-of-turn production
        /// </summary>
        /// <returns>True if a new army has been produced; otherwise false</returns>
        public bool Produce()
        {
            if (!ProducingArmy())
            {
                return false;
            }

            // Train the army!
            this.ArmyInTraining.TurnsToProduce--;

            // Is production complete?
            if (this.ArmyInTraining.TurnsToProduce == 0)
            {
                // Do we need to deliver this to another city?
                if (this.ArmyInTraining.DestinationCity != null)
                {
                    if (this.ArmiesToDeliver == null)
                    {
                        this.ArmiesToDeliver = new Queue<ArmyInTraining>();
                    }
                    this.ArmiesToDeliver.Enqueue(ArmyInTraining);                    
                }
                else
                {
                    Deploy(this.ArmyInTraining);
                }

                // TODO: Option to renew production?
                ArmyInTraining = null;
            }

            return !ProducingArmy();
        }

        private void Deploy(ArmyInTraining army)
        {
            if (army is null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            // TODO: Need to select the next open tile if tile is full
            Tile targetTile;
            if (army.DestinationCity == null)
            {
                targetTile = city.Tile;
            }
            else
            {
                targetTile = army.DestinationCity.Tile;
            }

            army.DisplayName = GetArmyDisplayName(army.ArmyInfo);

            Player.ConscriptArmy(army, targetTile);
        }

        /// <summary>
        /// Display name is a combo of army type and city of origin.
        /// </summary>
        /// <returns>Army name</returns>
        /// <remarks>Example: Marthos 1st Cavalry</remarks>
        private string GetArmyDisplayName(ArmyInfo info)
        {
            var pi = FindProductionInfo(info);
            var numberCreated = productionInfoDictionary[pi];
            string suffix;
            
            // Add the "st/nd/rd/th" to the number for style
            switch (numberCreated % 10)
            {
                case 1:
                    suffix = "st";
                    break;
                case 2:
                    suffix = "nd";
                    break;
                case 3:
                    suffix = "rd";
                    break;
                default:
                    suffix = "th";
                    break;
            }

            return $"{city.DisplayName} {numberCreated}{suffix} {info.DisplayName}";
        }

        /// <summary>
        /// Delivers armies to their destination.
        /// </summary>
        /// <returns>True if an army has been delivered; otherwise, False</returns>
        public bool Deliver()
        {
            bool delivered = false;

            if (!HasDeliveries())
            {
                return false;
            }

            // Move armies closer to their destination
            foreach (var army in ArmiesToDeliver)
            {
                army.TurnsToDeliver--;
            }

            // Deliver if armies have reached their destination
            if (HasDeliveries() &&
                ArmiesToDeliver.Peek().TurnsToDeliver == 0)
            {
                var armyInTraining = ArmiesToDeliver.Dequeue();
                Deploy(armyInTraining);
                delivered = true;
            }

            return delivered;
        }

        public bool HasDeliveries()
        {
            return ArmiesToDeliver != null && ArmiesToDeliver.Count > 0;
        }

        private bool PlayerHasSufficientGold(ArmyInfo armyInfo)
        {
            var pi = FindProductionInfo(armyInfo);
            return Player.Gold >= pi.Upkeep;
        }

        private void ChargePlayerForProductionCosts(ArmyInfo armyInfo)
        {
            var pi = FindProductionInfo(armyInfo);
            Player.Gold -= pi.Upkeep;
        }

        internal void Reset()
        {
            if (ProducingArmy())
            {
                StopProduction();
            }

            ArmiesToDeliver = null;
        }

        private bool CanProduce(ArmyInfo armyInfo)
        {
            return productionInfoDictionary.Any(piEntry => 
                piEntry.Key.ArmyInfoName == armyInfo.ShortName);
        }

        internal void CancelDelivery(City city)
        {
            if (HasDeliveries() &&
                ArmiesToDeliver.Any(a => a.DestinationCity == city))
            {
                // Cancel deliveries to city
                var newQueue = new Queue<ArmyInTraining>();
                while (newQueue.Count > 0)
                {
                    var army = ArmiesToDeliver.Dequeue();
                    if (army.DestinationCity != city)
                    {
                        newQueue.Enqueue(army);
                    }
                }

                ArmiesToDeliver = (newQueue.Count == 0) ? null : newQueue;                
            }
        }

        public void StopProduction()
        {
            ArmyInTraining = null;
        }

        public bool ProducingArmy()
        {
            return (ArmyInTraining != null);
        }

        private ProductionInfo FindProductionInfo(ArmyInfo armyInfo)
        {
            foreach (var pi in productionInfoDictionary.Keys)
            {
                if (pi.ArmyInfoName == armyInfo.ShortName)
                {
                    return pi;
                }
            }

            return null;
        }
    }
}