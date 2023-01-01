using System;
using System.Collections.Generic;
using System.Reflection;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;
using Wism.Client.War;

namespace Wism.Client.Factories
{
    public static class GameFactory
    {
        public static Game Create(GameEntity settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Game.CreateEmpty();

            // Game settings
            Game.Current.Random = new Random(settings.Random.Seed);
            Game.Current.WarStrategy = GetWarStrategy(settings.WarStrategy);
            Game.Current.TraversalStrategy = GetTraversalStrategy(settings.TraversalStrategies);
            Game.Current.MovementCoordinator = GetMovementCoordinator(settings.MovementStrategies);
            Game.Current.PathingStrategy = GetPathingStrategy(settings.PathingStrategy);
            Game.Current.Transition(settings.GameState);

            CreatePlayers(settings, Game.Current);

            _ = WorldFactory.Create(settings.World);

            return Game.Current;
        }

        public static Game Load(GameEntity snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            Game.CreateEmpty();

            // Game settings
            Game.Current.Random = LoadRandom(snapshot.Random);
            Game.Current.WarStrategy = GetWarStrategy(snapshot.WarStrategy);
            Game.Current.TraversalStrategy = GetTraversalStrategy(snapshot.TraversalStrategies);
            Game.Current.MovementCoordinator = GetMovementCoordinator(snapshot.MovementStrategies);
            Game.Current.PathingStrategy = GetPathingStrategy(snapshot.PathingStrategy);
            Game.Current.Transition(snapshot.GameState);
            Game.Current.CurrentPlayerIndex = snapshot.CurrentPlayerIndex;

            LoadPlayers(snapshot, Game.Current,
                out Dictionary<string, Player> cityToPlayer,
                out Dictionary<string, Player> capitolToPlayer);

            var world = WorldFactory.Load(snapshot.World,
                out Dictionary<int, Tile> armiesToTile,
                out Dictionary<int, Tile> visitingToTile);

            LoadCities(snapshot, world, cityToPlayer, capitolToPlayer);
            LoadArmies(snapshot, armiesToTile, visitingToTile);

            // Factory state
            ArmyFactory.LastId = snapshot.LastArmyId;


            return Game.Current;
        }

        private static T CreateObject<T>(AssemblyEntity entity)
        {
            var assembly = Assembly.Load(entity.AssemblyName);
            return (T)assembly.CreateInstance(entity.TypeName);
        }

        private static IWarStrategy GetWarStrategy(AssemblyEntity entity)
        {
            IWarStrategy warStrategy;

            try
            {
                warStrategy = CreateObject<IWarStrategy>(entity);
            }
            catch
            {
                // Use default to protect against missing or corrupt mods
                warStrategy = new DefaultWarStrategy();
            }

            return warStrategy;
        }

        private static IPathingStrategy GetPathingStrategy(AssemblyEntity entity)
        {
            IPathingStrategy strategy;

            try
            {
                strategy = CreateObject<IPathingStrategy>(entity);
            }
            catch
            {
                // Use default to protect against missing or corrupt mods
                strategy = new DijkstraPathingStrategy();
            }

            return strategy;
        }

        private static ITraversalStrategy GetTraversalStrategy(AssemblyEntity[] traversalEntities)
        {
            CompositeTraversalStrategy traversalStrategy;

            try
            {
                List<ITraversalStrategy> strategies = new List<ITraversalStrategy>();
                for (int i = 0; i < traversalEntities.Length; i++)
                {
                    var strategyEntity = traversalEntities[i];
                    var strategy = CreateObject<ITraversalStrategy>(strategyEntity);
                    strategies.Add(strategy);
                }

                traversalStrategy = new CompositeTraversalStrategy(strategies);
            }
            catch
            {
                // Use default to protect against missing or corrupt mods
                traversalStrategy = CompositeTraversalStrategy.CreateDefault();
            }

            return traversalStrategy;
        }

        private static MovementStrategyCoordinator GetMovementCoordinator(AssemblyEntity[] strategyEntities)
        {
            MovementStrategyCoordinator movementCoordinator;

            try
            {
                List<IMovementStrategy> strategies = new List<IMovementStrategy>();
                for (int i = 0; i < strategyEntities.Length; i++)
                {
                    var strategyEntity = strategyEntities[i];
                    var strategy = CreateObject<IMovementStrategy>(strategyEntity);
                    strategies.Add(strategy);
                }

                movementCoordinator = new MovementStrategyCoordinator(strategies);
            }
            catch
            {
                // Use default to protect against missing or corrupt mods
                movementCoordinator = MovementStrategyCoordinator.CreateDefault();
            }

            return movementCoordinator;
        }

        private static void CreatePlayers(GameEntity settings, Game current)
        {
            var players = settings.Players;
            current.Players = new List<Player>();
            for (int i = 0; i < players.Length; i++)
            {
                current.Players.Add(PlayerFactory.Create(players[i]));
            }
        }

        private static void LoadArmies(GameEntity snapshot, Dictionary<int, Tile> armiesToTile, Dictionary<int, Tile> visitingToTile)
        {
            var selectedArmies = new List<Army>();
            var allArmies = Game.Current.GetAllArmies();
            foreach (var army in allArmies)
            {
                // Tiles for armies and visiting armies
                if (armiesToTile.ContainsKey(army.Id))
                {
                    var tile = armiesToTile[army.Id];
                    tile.AddArmies(new List<Army>() { army });
                }
                else if (visitingToTile.ContainsKey(army.Id))
                {
                    var tile = visitingToTile[army.Id];
                    tile.AddVisitingArmies(new List<Army>() { army });
                }

                // Selected armies
                if (snapshot.SelectedArmyIds != null)
                {
                    for (int i = 0; i < snapshot.SelectedArmyIds.Length; i++)
                    {
                        if (army.Id == snapshot.SelectedArmyIds[i])
                        {
                            selectedArmies.Add(army);
                            break;
                        }
                    }

                    // Select armies: need to side-load since they were saved as Visiting Armies
                    if (selectedArmies.Count > 0)
                    {
                        Game.Current.SelectArmiesInternal(selectedArmies);
                    }
                }
            }
        }

        private static void LoadCities(GameEntity snapshot, World world,
            Dictionary<string, Player> cityToPlayer,
            Dictionary<string, Player> capitolToPlayer)
        {
            if (snapshot.World.Cities != null)
            {
                foreach (var citySnapshot in snapshot.World.Cities)
                {
                    var city = CityFactory.Load(citySnapshot, world);

                    // Add late-bound properties
                    if (cityToPlayer.ContainsKey(city.ShortName))
                    {
                        cityToPlayer[city.ShortName].AddCity(city);
                    }

                    if (capitolToPlayer.ContainsKey(city.ShortName))
                    {
                        capitolToPlayer[city.ShortName].Capitol = city;
                    }
                }
            }
        }

        /// <summary>
        /// Loads the random seed array into a new Random instance
        /// </summary>
        /// <param name="snapshot">RandomEntity to load</param>
        /// <returns>New Random based on the snapshot</returns>
        /// <remarks>
        /// This method overwrites the private seed array from Random. The seed in this 
        /// case is actually unused, but it is set for consistency.
        /// </remarks>
        private static Random LoadRandom(RandomEntity snapshot)
        {
            var random = new Random(snapshot.Seed);
            //if (snapshot.SeedArray != null)
            //{
            //    var seedArrayCopy = (int[])snapshot.SeedArray.Clone();
            //    var seedArrayInfo = typeof(Random).GetField("SeedArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            //    seedArrayInfo.SetValue(random, seedArrayCopy);
            //}

            return random;
        }

        private static void LoadPlayers(GameEntity snapshot, Game current,
            out Dictionary<string, Player> cityToPlayer,
            out Dictionary<string, Player> capitolToPlayer)
        {
            cityToPlayer = new Dictionary<string, Player>();
            capitolToPlayer = new Dictionary<string, Player>();

            var players = snapshot.Players;
            current.Players = new List<Player>();
            for (int i = 0; i < players.Length; i++)
            {
                current.Players.Add(PlayerFactory.Load(players[i],
                    out cityToPlayer,
                    out capitolToPlayer));
            }
        }
    }
}
