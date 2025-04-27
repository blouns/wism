using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.DeploymentStrategies;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core
{
    public class Barracks
    {
        private const int DefaultTurnsToDeliver = 3;

        private readonly City city;
        private readonly IDeploymentStrategy deploymentStrategy = new DefaultDeploymentStrategy();
        private readonly Dictionary<ProductionInfo, int> productionInfoDictionary;

        public Barracks(City city, ProductionInfo[] productionInfos)
        {
            this.city = city ?? throw new ArgumentNullException(nameof(city));

            if (productionInfos is null)
            {
                throw new ArgumentNullException(nameof(productionInfos));
            }

            this.productionInfoDictionary = new Dictionary<ProductionInfo, int>();
            for (var i = 0; i < productionInfos.Length; i++)
            {
                this.productionInfoDictionary.Add(productionInfos[i], 0);
            }
        }

        public ArmyInTraining ArmyInTraining { get; set; }

        public Queue<ArmyInTraining> ArmiesToDeliver { get; set; }

        public Player Player => this.city.Player;

        public int GetProductionNumber(string armyInfoName)
        {
            if (string.IsNullOrWhiteSpace(armyInfoName))
            {
                throw new ArgumentException($"'{nameof(armyInfoName)}' cannot be null or whitespace",
                    nameof(armyInfoName));
            }

            foreach (var pi in this.productionInfoDictionary.Keys)
            {
                if (pi.ArmyInfoName.ToLowerInvariant() == armyInfoName.ToLowerInvariant())
                {
                    return this.productionInfoDictionary[pi];
                }
            }

            throw new ArgumentOutOfRangeException("armyInfo", "Could not find a matching production info.");
        }

        public void SetProductionNumber(string armyInfoName, int productionNumber)
        {
            if (string.IsNullOrWhiteSpace(armyInfoName))
            {
                throw new ArgumentException($"'{nameof(armyInfoName)}' cannot be null or whitespace",
                    nameof(armyInfoName));
            }

            ProductionInfo slot = null;
            foreach (var pi in this.productionInfoDictionary.Keys)
            {
                if (pi.ArmyInfoName.ToLowerInvariant() == armyInfoName.ToLowerInvariant())
                {
                    slot = pi;
                }
            }

            if (slot == null)
            {
                throw new ArgumentOutOfRangeException("armyInfo", "Could not find a matching production info.");
            }

            this.productionInfoDictionary[slot] = productionNumber;
        }

        public List<ProductionInfo> GetProductionKinds()
        {
            return new List<ProductionInfo>(this.productionInfoDictionary.Keys);
        }

        /// <summary>
        ///     Start producing an army in the destination city
        /// </summary>
        /// <param name="armyInfo">Army kind to produce</param>
        /// <param name="destinationCity">City to deliver to (or null for current city)</param>
        /// <returns></returns>
        public bool StartProduction(ArmyInfo armyInfo, City destinationCity = null)
        {
            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (!this.CanProduce(armyInfo))
            {
                throw new ArgumentException($"This barracks cannot create {armyInfo.ShortName}");
            }

            if (!this.PlayerHasSufficientGold(armyInfo))
            {
                return false;
            }

            this.ChargePlayerForProductionCosts(armyInfo);

            // Increment the number of armies of this type built
            var pi = this.FindProductionInfo(armyInfo);
            this.productionInfoDictionary[pi]++;

            this.ArmyInTraining = new ArmyInTraining
            {
                ArmyInfo = armyInfo,
                DestinationCity = destinationCity,
                ProductionCity = this.city,
                TurnsToProduce = pi.TurnsToProduce,
                TurnsToDeliver = DefaultTurnsToDeliver,
                Upkeep = pi.Upkeep,
                Moves = pi.Moves,
                Strength = pi.Strength
            };


            return true;
        }

        /// <summary>
        ///     Perform beginning-of-turn production
        /// </summary>
        /// <returns>True if a new army has been produced; otherwise false</returns>
        public bool Produce(out ArmyInTraining armyProduced)
        {
            armyProduced = null;

            if (!this.ProducingArmy())
            {
                return false;
            }

            // Train the army!
            this.ArmyInTraining.TurnsToProduce--;

            // Is production complete?
            if (this.ArmyInTraining.TurnsToProduce == 0)
            {
                armyProduced = this.ArmyInTraining;

                // Do we need to deliver this to another city?
                if (this.ArmyInTraining.DestinationCity != null)
                {
                    if (this.ArmiesToDeliver == null)
                    {
                        this.ArmiesToDeliver = new Queue<ArmyInTraining>();
                    }

                    this.ArmiesToDeliver.Enqueue(this.ArmyInTraining);
                }
                else
                {
                    this.Deploy(this.ArmyInTraining);
                }

                this.ArmyInTraining = null;
            }

            return armyProduced != null;
        }

        private void Deploy(ArmyInTraining army)
        {
            if (army is null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            Tile targetTile;
            if (army.DestinationCity == null)
            {
                targetTile = this.city.Tile;
            }
            else
            {
                targetTile = army.DestinationCity.Tile;
            }

            // Select the next open tile if army cannot be placed here (full, navy)
            targetTile = this.deploymentStrategy.FindNextOpenTile(this.Player, army.ArmyInfo, targetTile);

            army.DisplayName = this.GetArmyDisplayName(army.ArmyInfo);

            this.Player.ConscriptArmy(army, targetTile);
        }

        /// <summary>
        ///     Display name is a combo of army type and city of origin.
        /// </summary>
        /// <returns>Army name</returns>
        /// <remarks>Example: Marthos 1st Cavalry</remarks>
        private string GetArmyDisplayName(ArmyInfo info)
        {
            var pi = this.FindProductionInfo(info);
            var numberCreated = this.productionInfoDictionary[pi];
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

            return $"{this.city.DisplayName} {numberCreated}{suffix} {info.DisplayName}";
        }

        /// <summary>
        ///     Delivers armies to their destination.
        /// </summary>
        /// <returns>True if an army has been delivered; otherwise, False</returns>
        public bool Deliver(out ArmyInTraining armyDelivered)
        {
            armyDelivered = null;
            var delivered = false;

            if (!this.HasDeliveries())
            {
                return false;
            }

            // Move armies closer to their destination
            foreach (var army in this.ArmiesToDeliver)
            {
                army.TurnsToDeliver--;
            }

            // Deliver if armies have reached their destination
            if (this.HasDeliveries() &&
                this.ArmiesToDeliver.Peek().TurnsToDeliver == 0)
            {
                var armyInTraining = this.ArmiesToDeliver.Dequeue();
                this.Deploy(armyInTraining);
                armyDelivered = armyInTraining;
                delivered = true;
            }

            return delivered;
        }

        public bool HasDeliveries()
        {
            return this.ArmiesToDeliver != null && this.ArmiesToDeliver.Count > 0;
        }

        private bool PlayerHasSufficientGold(ArmyInfo armyInfo)
        {
            var pi = this.FindProductionInfo(armyInfo);
            return this.Player.Gold >= pi.Upkeep;
        }

        private void ChargePlayerForProductionCosts(ArmyInfo armyInfo)
        {
            var pi = this.FindProductionInfo(armyInfo);
            this.Player.Gold -= pi.Upkeep;
        }

        internal void Reset()
        {
            if (this.ProducingArmy())
            {
                this.StopProduction();
            }

            this.ArmiesToDeliver = null;
        }

        private bool CanProduce(ArmyInfo armyInfo)
        {
            return this.productionInfoDictionary.Any(piEntry =>
                piEntry.Key.ArmyInfoName == armyInfo.ShortName);
        }

        internal void CancelDelivery(City city)
        {
            if (this.HasDeliveries() &&
                this.ArmiesToDeliver.Any(a => a.DestinationCity == city))
            {
                // Cancel deliveries to city
                var newQueue = new Queue<ArmyInTraining>();
                while (newQueue.Count > 0)
                {
                    var army = this.ArmiesToDeliver.Dequeue();
                    if (army.DestinationCity != city)
                    {
                        newQueue.Enqueue(army);
                    }
                }

                this.ArmiesToDeliver = newQueue.Count == 0 ? null : newQueue;
            }
        }

        public void StopProduction()
        {
            this.ArmyInTraining = null;
        }

        public bool ProducingArmy()
        {
            return this.ArmyInTraining != null;
        }

        private ProductionInfo FindProductionInfo(ArmyInfo armyInfo)
        {
            foreach (var pi in this.productionInfoDictionary.Keys)
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