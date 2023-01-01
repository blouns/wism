using System;
using System.Collections.Generic;
using Wism.Client.AI.Adapta.TacticalModules;
using Wism.Client.AI.Task;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.Strategic
{
    public class AssetAllocationModule
    {
        public World World { get; }
        public Player Player { get; }
        public List<TacticalModule> TacticalModules { get => tacticalModules; set => tacticalModules = value; }

        private TargetPortfolio targets = new TargetPortfolio();
        private List<Army> myArmies = new List<Army>();
        private List<City> myCities = new List<City>();
        private readonly ILogger logger;

        private List<TacticalModule> tacticalModules = new List<TacticalModule>();        

        public static AssetAllocationModule CreateDefault(ControllerProvider provider, World world, Player player, ILogger logger)
        {
            var allocator = new AssetAllocationModule(world, player, logger);

            // Add default modules
            allocator.TacticalModules.Add(new ConquerNeutralCitiesTactic(provider));

            return allocator;
        }

        private AssetAllocationModule(World world, Player player, ILogger logger)
        {
            this.World = world ?? throw new ArgumentNullException(nameof(world));
            this.Player = player ?? throw new ArgumentNullException(nameof(player));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private Dictionary<TacticalModule, Dictionary<Army, Bid>> AcceptBids()
        {
            var bidsByModule = new Dictionary<TacticalModule, Dictionary<Army, Bid>>();
            foreach (var module in this.tacticalModules)
            {
                bidsByModule.Add(module, module.GenerateArmyBids(this.myArmies, this.myCities, this.targets));
            }

            return bidsByModule;
        }

        private void GatherTargets()
        {
            // TODO: Only create tasks for relevant doers (i.e. if i have no heros, then locations are useless)
            var ti = new TargetIntelligence(World);
            var targets = ti.FindTargetObjects(Player);

            logger.LogInformation($"Detected {targets.LooseItems} loose items.");
            logger.LogInformation($"Detected {targets.OpposingCities} opposing cities.");
            logger.LogInformation($"Detected {targets.NeutralCities} opposing cities.");
            logger.LogInformation($"Detected {targets.OpposingArmies} opposing armies.");
            logger.LogInformation($"Detected {targets.UnsearchedLocations} unsearched locations.");

            this.targets = targets;
        }

        private void GatherAssets()
        {
            myArmies = Player.GetArmies();
            myCities = Player.GetCities();

            logger.LogInformation($"Detected {myArmies.Count} friendly armies.");
            logger.LogInformation($"Detected {myCities.Count} friendly cities.");
        }

        //public List<Bid> SelectBids()
        //{
        //    List<Bid> winningBids = new List<Bid>();

        //    GatherAssets();
        //    GatherTargets();

        //    logger.LogInformation($"Accepting bids from tactical modules...");
        //    var bidsByModule = AcceptBids();

        //    // TODO: Fix once there is more than one module
        //    //logger.LogInformation($"Evaluating {bidsByModule.Count} bids...");
        //    //var winningBids = EvaluateBids(bidsByModule);

        //    // HACK: Just get something going for now
        //    //List<Bid> bids = new List<Bid>();
        //    foreach (var module in bidsByModule.Keys)
        //    {
        //        //winningBids = new Dictionary<Army, Bid>();
        //        foreach (var army in bidsByModule[module].Keys)
        //        {
        //            //winningBids.Add(bidsByModule[module][army]);
        //            winningBids.Add(army, bidsByModule[module][army]);
        //        }
        //    }

        //    return winningBids;
        //}
       
        private List<Bid> EvaluateBids(Dictionary<TacticalModule, Dictionary<Army, Bid>> bidsByModule)
        {
            // Only one module for now so just skip this step
            return null;

            //return this.UtilityManager.SelectWinner(bidsByModule, this.myArmies, this.myCities, this.targets);
        }

        internal Dictionary<TacticalModule, Dictionary<Army, Bid>> Allocate()
        {
            List<Bid> winningBids = new List<Bid>();

            GatherAssets();
            GatherTargets();

            logger.LogInformation($"Accepting bids from tactical modules...");
            return AcceptBids();
        }
    }
}
