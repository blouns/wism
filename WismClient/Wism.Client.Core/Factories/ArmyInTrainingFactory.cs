using System;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    public class ArmyInTrainingFactory
    {
        public static ArmyInTraining Load(ArmyInTrainingEntity snapshot, World world)
        {
            if (snapshot == null)
            {
                return null;
            }

            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var ait = new ArmyInTraining
            {
                ArmyInfo = ModFactory.FindArmyInfo(snapshot.ArmyShortName),
                DestinationCity = string.IsNullOrWhiteSpace(snapshot.DestinationCityShortName)
                    ? null
                    : world.FindCity(snapshot.DestinationCityShortName),
                ProductionCity = world.FindCity(snapshot.ProductionCityShortName),
                TurnsToProduce = snapshot.TurnsToProduce,
                TurnsToDeliver = snapshot.TurnsToDeliver,
                Upkeep = snapshot.Upkeep,
                Moves = snapshot.Moves,
                Strength = snapshot.Strength,
                DisplayName = snapshot.DisplayName
            };

            return ait;
        }
    }
}