using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Commands;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.AI.Strategic
{
    public class ProductionModule : ITurnModule
    {
        private const int MinimumDestinationDistanceGain = 4;
        private const int MaximumVectorDestinationLoad = 3;
        private const int LongRangePressureDistance = 8;

        private readonly CityController cityController;
        private readonly CityTargetEvaluator cityTargetEvaluator;
        private readonly IWismLogger logger;
        private readonly HashSet<string> handledTurns = new HashSet<string>();

        public ProductionModule(CityController cityController, IWismLogger logger)
            : this(cityController, new CityTargetEvaluator(), logger)
        {
        }

        public ProductionModule(CityController cityController, CityTargetEvaluator cityTargetEvaluator, IWismLogger logger)
        {
            this.cityController = cityController;
            this.cityTargetEvaluator = cityTargetEvaluator;
            this.logger = logger;
        }

        public IEnumerable<ICommandAction> GenerateCommands(World world)
        {
            var commands = new List<ICommandAction>();
            var player = Game.Current.GetCurrentPlayer();
            if (player == null || player.IsHuman || player.IsDead)
            {
                return commands;
            }

            var turnKey = string.Format("{0}:{1}", player.Clan.ShortName, player.Turn);
            if (handledTurns.Contains(turnKey))
            {
                return commands;
            }

            handledTurns.Add(turnKey);

            var review = new ReviewProductionCommand(cityController, player);
            commands.Add(review);
            commands.Add(new RenewProductionCommand(cityController, player, review));

            var ownedCities = player.GetCities()
                .Where(c => c != null)
                .ToList();
            var pressureTargets = world.GetCities()
                .Where(c => c != null && c.Clan != player.Clan)
                .ToList();

            foreach (var city in ownedCities.Where(c => !c.Barracks.ProducingArmy()))
            {
                var pressureTarget = SelectPressureTarget(city, pressureTargets);
                var destination = ChooseDestination(city, ownedCities, pressureTargets, pressureTarget);
                var production = ChooseProduction(city, pressureTarget, destination);
                if (production == null)
                {
                    continue;
                }

                var armyInfo = ModFactory.FindArmyInfo(production.ArmyInfoName);
                if (armyInfo == null)
                {
                    continue;
                }

                commands.Add(new StartProductionCommand(cityController, city, armyInfo, destination));

                var destinationText = destination == null ? city.ShortName : destination.ShortName;
                logger.LogInformation(string.Format("[Production] {0} starting {1} in {2} for {3}.", player.Clan.ShortName, armyInfo.ShortName, city.ShortName, destinationText));
            }

            return commands;
        }

        private City ChooseDestination(
            City productionCity,
            List<City> ownedCities,
            List<City> pressureTargets,
            City pressureTarget = null)
        {
            if (productionCity == null || ownedCities == null || ownedCities.Count < 2 || pressureTargets == null || pressureTargets.Count == 0)
            {
                return null;
            }

            var target = pressureTarget ?? SelectPressureTarget(productionCity, pressureTargets);
            if (target == null)
            {
                return null;
            }

            var productionDistance = this.cityTargetEvaluator.GetDistanceToCity(productionCity.Tile, target);
            var destination = ownedCities
                .Where(city => city != null && city != productionCity)
                .Select(city => new
                {
                    City = city,
                    DistanceToTarget = this.cityTargetEvaluator.GetDistanceToCity(city.Tile, target),
                    DistanceFromProduction = this.cityTargetEvaluator.GetDistanceToCity(productionCity.Tile, city)
                })
                .Where(candidate => candidate.DistanceToTarget < productionDistance)
                .Where(candidate => GetDestinationLoad(candidate.City, ownedCities) < MaximumVectorDestinationLoad)
                .OrderBy(candidate => candidate.DistanceToTarget)
                .ThenBy(candidate => candidate.DistanceFromProduction)
                .ThenBy(candidate => candidate.City.ShortName)
                .FirstOrDefault();

            if (destination == null)
            {
                return null;
            }

            return productionDistance - destination.DistanceToTarget >= MinimumDestinationDistanceGain
                ? destination.City
                : null;
        }

        private static int GetDestinationLoad(City destination, List<City> ownedCities)
        {
            var garrison = destination.MusterArmies()
                .Count(army => army.Clan == destination.Clan);
            var incoming = ownedCities
                .Where(city => city?.Barracks != null)
                .SelectMany(city => city.Barracks.ArmiesToDeliver == null
                    ? Enumerable.Empty<ArmyInTraining>()
                    : city.Barracks.ArmiesToDeliver)
                .Count(army => army.DestinationCity == destination);
            var trainingForDestination = ownedCities
                .Where(city => city?.Barracks?.ArmyInTraining?.DestinationCity == destination)
                .Count();

            return garrison + incoming + trainingForDestination;
        }

        private City SelectPressureTarget(City productionCity, List<City> pressureTargets)
        {
            return pressureTargets
                .Where(city => city?.Tile != null)
                .Select(city => new
                {
                    City = city,
                    Score = ScorePressureTarget(productionCity, city),
                    Distance = this.cityTargetEvaluator.GetDistanceToCity(productionCity.Tile, city)
                })
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.City.ShortName)
                .Select(candidate => candidate.City)
                .FirstOrDefault();
        }

        private double ScorePressureTarget(City productionCity, City target)
        {
            var distance = this.cityTargetEvaluator.GetDistanceToCity(productionCity.Tile, target);
            var value = 1.0 + (target.Income / 20.0) + (target.Defense / 20.0);
            var owner = target.Clan?.Player;

            if (target.Clan == null || target.Clan.ShortName == "Neutral")
            {
                value += 1.25;
            }
            else
            {
                value += 2.0;
            }

            if (owner != null)
            {
                if (owner.GetCities().Count == 1)
                {
                    value += 12.0;
                }

                if (owner.Capitol == target)
                {
                    value += 1.5;
                }
            }

            if (!target.MusterArmies().Any(army => army.Clan != productionCity.Clan))
            {
                value += 2.0;
            }

            return value / (distance + 1);
        }

        private ProductionInfo ChooseProduction(City city, City pressureTarget, City destination)
        {
            var useMobilityDoctrine = ShouldUseMobilityDoctrine(city, pressureTarget, destination);
            return city.Barracks.GetProductionKinds()
                .OrderByDescending(info => ProductionScore(info, useMobilityDoctrine))
                .ThenBy(info => info.TurnsToProduce)
                .ThenBy(info => info.Upkeep)
                .ThenBy(info => info.ArmyInfoName)
                .FirstOrDefault();
        }

        private bool ShouldUseMobilityDoctrine(City city, City pressureTarget, City destination)
        {
            if (destination != null && destination != city)
            {
                return true;
            }

            if (city?.Tile == null || pressureTarget == null)
            {
                return false;
            }

            return this.cityTargetEvaluator.GetDistanceToCity(city.Tile, pressureTarget) >= LongRangePressureDistance;
        }

        private static double ProductionScore(ProductionInfo info, bool useMobilityDoctrine)
        {
            return useMobilityDoctrine
                ? MobilityProductionScore(info)
                : EfficientProductionScore(info);
        }

        private static double EfficientProductionScore(ProductionInfo info)
        {
            var turns = info.TurnsToProduce <= 0 ? 1 : info.TurnsToProduce;
            var upkeep = info.Upkeep <= 0 ? 1 : info.Upkeep;
            return ((info.Strength * 3.0) + info.Moves) / (turns + (upkeep / 10.0));
        }

        private static double MobilityProductionScore(ProductionInfo info)
        {
            var turns = info.TurnsToProduce <= 0 ? 1 : info.TurnsToProduce;
            var upkeep = info.Upkeep <= 0 ? 1 : info.Upkeep;
            var armyInfo = ModFactory.FindArmyInfo(info.ArmyInfoName);
            var mobility = armyInfo?.Moves ?? info.Moves;
            var strength = armyInfo?.Strength ?? info.Strength;
            var score = (strength * 2.0) + (mobility * 3.0) - (turns * 2.0) - (upkeep / 4.0);

            if (armyInfo != null && !armyInfo.CanWalk && !armyInfo.CanFly)
            {
                score -= 20.0;
            }

            return score;
        }
    }
}
