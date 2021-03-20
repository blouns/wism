using System;
using System.Collections.Generic;
using System.Reflection;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Wism.Client.Data
{
    public static class GamePersistance
    {
        public static GameEntity SnapshotGame(Game game)
        {
            GameEntity snapshot = new GameEntity()
            {
                Timestamp = DateTime.UtcNow,
                CurrentPlayerIndex = game.CurrentPlayerIndex,
                GameState = game.GameState,
                Players = SnapshotPlayers(game.Players),
                Random = SnapshotRandom(game.Random, game),
                SelectedArmyIds = SnapshotSelectedArmies(game.GetSelectedArmies()),
                WarStrategy = SnapshotWarStrategy(game.WarStrategy),
                World = SnapshotWorld(World.Current),
                LastArmyId = ArmyFactory.LastId                
            };

            return snapshot;
        }

        private static WarStrategyEntity SnapshotWarStrategy(IWarStrategy warStrategy)
        {
            var snapshot = new WarStrategyEntity()
            {
                AssemblyName = Assembly.GetAssembly(warStrategy.GetType()).FullName,
                TypeName = warStrategy.GetType().FullName
            };

            return snapshot;
        }

        private static int[] SnapshotSelectedArmies(List<Army> selectedArmies)
        {
            if (selectedArmies == null || selectedArmies.Count == 0)
            {
                return null;
            }

            return selectedArmies.ConvertAll<int>(a => a.Id).ToArray();
        }

        private static WorldEntity SnapshotWorld(World world)
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            int xBound = world.Map.GetUpperBound(0) + 1;
            int yBound = world.Map.GetUpperBound(1) + 1;
            var snapshot = new WorldEntity()
            {
                Name = world.Name,
                Cities = SnapshotCities(world),
                Locations = SnapshotLocations(world),
                MapXUpperBound = xBound,
                MapYUpperBound = yBound,
                Tiles = SnapshotTiles(world, xBound, yBound)
            };

            return snapshot;
        }

        private static LocationEntity[] SnapshotLocations(World world)
        {
            var locations = world.GetLocations();
            if (locations.Count == 0)
            {
                return null;
            }

            var snapshot = new LocationEntity[locations.Count];
            for (int i = 0; i < locations.Count; i++)
            {
                snapshot[i] = new LocationEntity()
                {
                    Boon = SnapshotBoon(locations[i].Boon),
                    Id = locations[i].Id,
                    LocationShortName = locations[i].ShortName,
                    Monster = locations[i].Monster,
                    Searched = locations[i].Searched,
                    X = locations[i].X,
                    Y = locations[i].Y
                };
            }

            return snapshot;
        }

        public static BoonEntity SnapshotBoon(IBoon boon)
        {
            if (boon == null)
            {
                return null;
            }

            var snapshot = new BoonEntity()
            {
                AlliesShortName = (boon is AlliesBoon) ? ((AlliesBoon)boon).ArmyInfo.ShortName : null,
                ArtifactShortName = (boon is ArtifactBoon) ? ((ArtifactBoon)boon).Artifact.ShortName : null,                
                BoonAssemblyName = Assembly.GetAssembly(boon.GetType()).FullName,
                BoonTypeName = boon.GetType().FullName
            };

            return snapshot;
        }

        private static TileEntity[] SnapshotTiles(World world, int xUpperBound, int yUpperBound)
        {
            var map = world.Map;
            var snapshot = new TileEntity[xUpperBound * yUpperBound];            

            for (int y = 0; y < yUpperBound; y++)
            {
                for (int x = 0; x < xUpperBound; x++)
                {
                    var tile = map[x, y];
                    snapshot[x + y * xUpperBound] = new TileEntity()
                    {
                        ArmyIds = ConvertToArmyIds(tile.Armies),
                        CityShortName = tile.HasCity() ? tile.City.ShortName : null,
                        Items = SnapshotItems(tile.Items),
                        LocationShortName = tile.HasLocation() ? tile.Location.ShortName : null,
                        TerrainShortName = tile.Terrain.ShortName,
                        VisitingArmyIds = ConvertToArmyIds(tile.VisitingArmies),
                        X = tile.X,
                        Y = tile.Y
                    };
                }
            }

            return snapshot;
        }

        private static ArtifactEntity[] SnapshotItems(List<Artifact> items)
        {
            if (items == null || items.Count == 0)
            {
                return null;
            }

            var snapshot = new ArtifactEntity[items.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = new ArtifactEntity()
                {
                    ArtifactShortName = items[i].ShortName,
                    Id = items[i].Id,
                    PlayerIndex = GetPlayerIndex(items[i]),
                    X = items[i].X,
                    Y = items[i].Y
                };
            }

            return snapshot;
        }

        private static int GetPlayerIndex(Artifact artifact)
        {
            for (int i = 0; i < Game.Current.Players.Count; i++)
            {
                if (artifact.Player != null)
                {
                    if (Game.Current.Players[i].Clan == artifact.Player.Clan)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static int[] ConvertToArmyIds(List<Army> armies)
        {
            if (armies == null || armies.Count == 0)
            {
                return null;
            }

            return armies.ConvertAll<int>(a => a.Id).ToArray();
        }

        private static int[] ConvertToHeroIds(List<Hero> heroes)
        {
            if (heroes == null || heroes.Count == 0)
            {
                return null;
            }

            return heroes.ConvertAll<int>(a => a.Id).ToArray();
        }

        private static CityEntity[] SnapshotCities(World world)
        {
            var cities = world.GetCities();
            if (cities == null || cities.Count == 0)
            {
                return null;
            }

            var snapshot = new CityEntity[cities.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = new CityEntity()
                {
                    ArmiesToDeliver = SnapshotArmiesToDeliver(cities[i].Barracks.ArmiesToDeliver),
                    ArmyInTraining = SnapshotArmyInTraining(cities[i].Barracks.ArmyInTraining),
                    ClanShortName = (cities[i].Clan == null) ? null : cities[i].Clan.ShortName,
                    CityShortName = cities[i].ShortName,
                    ProductionInfo = SnapshotProductionSlots(cities[i].Barracks),
                    Defense = cities[i].Defense,
                    Id = cities[i].Id,
                    X = cities[i].X,
                    Y = cities[i].Y
                };
            }

            return snapshot;
        }

        private static ProductionEntity SnapshotProductionSlots(Barracks barracks)
        {
            var productionSlots = barracks.GetProductionKinds();
            if (productionSlots == null || productionSlots.Count == 0)
            {
                return null;
            }

            var snapshot = new ProductionEntity()           
            {
                ArmyNames = ConvertToArmyShortNames(productionSlots),
                ProductionNumbers = ConvertToProductionNumbers(barracks, productionSlots)
            };

            return snapshot;
        }

        private static string[] ConvertToArmyShortNames(List<ProductionInfo> productionSlots)
        {
            if (productionSlots == null || productionSlots.Count == 0)
            {
                return null;
            }

            string[] armyNames = new string[productionSlots.Count];
            for (int i = 0; i < armyNames.Length; i++)
            {
                armyNames[i] = productionSlots[i].ArmyInfoName;
            }

            return armyNames;
        }

        private static int[] ConvertToProductionNumbers(Barracks barracks, List<ProductionInfo> productionSlots)
        {
            if (productionSlots == null || productionSlots.Count == 0)
            {
                return null;
            }

            int[] productionNumbers = new int[productionSlots.Count];
            for (int i = 0; i < productionNumbers.Length; i++)
            {
                productionNumbers[i] = barracks.GetProductionNumber(productionSlots[i].ArmyInfoName);
            }

            return productionNumbers;
        }

        private static ArmyInTrainingEntity SnapshotArmyInTraining(ArmyInTraining armyInTraining)
        {
            if (armyInTraining == null)
            {
                return null;
            }

            var snapshot = new ArmyInTrainingEntity()
            {
                ArmyShortName = armyInTraining.ArmyInfo.ShortName,
                DestinationCityShortName = (armyInTraining.DestinationCity == null) ? null : armyInTraining.DestinationCity.ShortName,
                DisplayName = armyInTraining.DisplayName,
                Moves = armyInTraining.Moves,
                ProductionCityShortName = armyInTraining.ProductionCity.ShortName,
                Strength = armyInTraining.Strength,
                TurnsToDeliver = armyInTraining.TurnsToDeliver,
                TurnsToProduce = armyInTraining.TurnsToProduce,
                Upkeep = armyInTraining.Upkeep
            };

            return snapshot;
        }

        private static ArmyInTrainingEntity[] SnapshotArmiesToDeliver(Queue<ArmyInTraining> armiesToDeliver)
        {
            if (armiesToDeliver == null || armiesToDeliver.Count == 0)
            {
                return null;
            }

            var snapshot = new ArmyInTrainingEntity[armiesToDeliver.Count];
            int i = 0;
            foreach (var ait in armiesToDeliver)
            {
                snapshot[i++] = SnapshotArmyInTraining(ait);
            };

            return snapshot;
        }

        private static PlayerEntity[] SnapshotPlayers(List<Player> players)
        {
            if (players == null || players.Count == 0)
            {
                return null;
            }

            var snapshot = new PlayerEntity[players.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = new PlayerEntity()
                {
                    Armies = SnapshotArmies(players[i].GetArmies()),
                    CapitolShortName = (players[i].Capitol != null) ? players[i].Capitol.ShortName : null,
                    ClanShortName = players[i].Clan.ShortName,
                    Gold = players[i].Gold,
                    IsDead = players[i].IsDead,
                    MyCitiesShortNames = ConvertToCityShortNames(players[i].GetCities()),
                    Turn = players[i].Turn,
                    LastHeroTurn = players[i].LastHeroTurn,
                    NewHeroPrice = players[i].NewHeroPrice,
                    MyHeroIds = ConvertToHeroIds(players[i].GetHeros())
                };
            }

            return snapshot;
        }

        private static string[] ConvertToCityShortNames(List<City> cities)
        {
            return cities.ConvertAll<string>(c => c.ShortName).ToArray();
        }

        private static ArmyEntity[] SnapshotArmies(List<Army> armies)
        {
            if (armies == null || armies.Count == 0)
            {
                return null;
            }

            var snapshot = new ArmyEntity[armies.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = new ArmyEntity()
                {
                    ArmyShortName = armies[i].ShortName,
                    Artifacts = SnapshotArtifacts(armies[i]),
                    BlessedAtShortNames = ConvertToLocationShortNames(armies[i].BlessedAt),
                    DisplayName = armies[i].DisplayName,
                    Id = armies[i].Id,
                    IsDead = armies[i].IsDead,
                    IsHero = armies[i] is Hero,
                    Moves = armies[i].Moves,
                    MovesRemaining = armies[i].MovesRemaining,
                    Strength = armies[i].Strength,
                    Upkeep = armies[i].Upkeep,
                    X = armies[i].X,
                    Y = armies[i].Y
                };
            }

            return snapshot;
        }

        private static string[] ConvertToLocationShortNames(List<Location> blessedAt)
        {
            if (blessedAt == null || blessedAt.Count == 0)
            {
                return null;
            }

            return blessedAt.ConvertAll<string>(l => l.ShortName).ToArray();
        }

        private static ArtifactEntity[] SnapshotArtifacts(Army army)
        {
            var hero = army as Hero;
            if (hero == null || !hero.HasItems())
            {
                return null;
            }

            var snapshot = new ArtifactEntity[hero.Items.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = new ArtifactEntity()
                {
                    ArtifactShortName = hero.Items[i].ShortName,
                    Id = hero.Items[i].Id,
                    PlayerIndex = GetPlayerIndex(hero.Items[i])
                };
            }

            return snapshot;
        }

        private static RandomEntity SnapshotRandom(Random random, Game game)
        {
            var snapshot = new RandomEntity();

            snapshot.Seed = game.RandomSeed;
            snapshot.Random = Cloner.Clone<Random>(random);

            return snapshot;
        }
    }
}
