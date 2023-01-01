using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.MovementStrategies;
using Wism.Client.Core.Armies.TerrainTraversalStrategies;
using Wism.Client.Core.Armies.WarStrategies;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Pathing;

namespace Wism.Client.Test.Common;

public static class TestGameFactory
{
    public const string DataPath = "Data";

    public static GameEntity CreateDefaultNewGameSettings(string worldName, int seed = 1990)
    {
        var settings = new GameEntity
        {
            Random = new RandomEntity { Seed = seed },
            WarStrategy = CreateDefaultWarStrategy(),
            PathingStrategy = CreatePathingStrategy(),
            TraversalStrategies = CreateTraversalStrategies(),
            MovementStrategies = CreateMovementStrategies(),
            Players = CreateDefaultPlayers(),
            World = CreateWorld(worldName),
            GameState = GameState.Ready
        };

        return settings;
    }

    private static WorldEntity CreateWorld(string worldName)
    {
        // Deserialize map from the world
        var entity = Deserialize<WorldEntity>($@"{DataPath}\{worldName}\Map.json");

        // Deserialize other objects
        entity.Cities = Deserialize<CityEntity[]>($@"{DataPath}\{worldName}\City.json");
        entity.Locations = Deserialize<LocationEntity[]>($@"{DataPath}\{worldName}\Location.json");

        return entity;
    }

    private static T Deserialize<T>(string path)
    {
        var json = File.ReadAllText(path);

        var jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        return JsonConvert.DeserializeObject<T>(json, jsonSettings);
    }

    private static PlayerEntity[] CreateDefaultPlayers()
    {
        return new[]
        {
            new()
            {
                ClanShortName = "Sirians",
                IsHuman = true
            },
            new PlayerEntity
            {
                ClanShortName = "LordBane",
                IsHuman = true
            }
        };
    }

    private static AssemblyEntity CreateDefaultWarStrategy()
    {
        var entity = new AssemblyEntity
        {
            AssemblyName = Assembly.GetAssembly(typeof(DefaultWarStrategy)).FullName,
            TypeName = typeof(DefaultWarStrategy).FullName
        };

        return entity;
    }

    private static AssemblyEntity[] CreateMovementStrategies()
    {
        var movementStrategy = MovementStrategyCoordinator.CreateDefault();

        return GamePersistance.SnapshotMovementStrategies(movementStrategy);
    }

    private static AssemblyEntity[] CreateTraversalStrategies()
    {
        var traversalStrategy = new CompositeTraversalStrategy(new List<ITraversalStrategy>
        {
            new StandardTraversalStrategy(),
            new HeroFlightTraversalStrategy(),
            new NavalTraversalStrategy()
        });

        return GamePersistance.SnapshotTraversalStrategies(traversalStrategy);
    }

    private static AssemblyEntity CreatePathingStrategy()
    {
        var entity = new AssemblyEntity
        {
            AssemblyName = Assembly.GetAssembly(typeof(AStarPathingStrategy)).FullName,
            TypeName = typeof(AStarPathingStrategy).FullName
        };

        return entity;
    }
}