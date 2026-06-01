using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Validation
{
    public sealed class WorldValidator
    {
        public WorldValidationReport Validate(World world)
        {
            var players = Game.IsInitialized() ? Game.Current.Players : null;
            return this.Validate(world, players);
        }

        public WorldValidationReport Validate(World world, IEnumerable<Player> players)
        {
            var issues = new List<WorldValidationIssue>();

            if (world == null)
            {
                issues.Add(new WorldValidationIssue("world.null", "World is null."));
                return new WorldValidationReport(issues);
            }

            if (world.Map == null)
            {
                issues.Add(new WorldValidationIssue("map.null", "World map is null."));
                return new WorldValidationReport(issues);
            }

            var width = world.Map.GetLength(0);
            var height = world.Map.GetLength(1);
            if (width < 2 || height < 2)
            {
                issues.Add(new WorldValidationIssue("map.too-small", $"World map must be at least 2x2; actual {width}x{height}."));
            }

            this.ValidateTiles(world, issues);
            this.ValidateCities(world, issues);
            this.ValidateLocations(world, issues);
            this.ValidateStacks(world, issues);
            this.ValidatePlayers(players, issues);
            this.ValidateReachability(world, issues);

            return new WorldValidationReport(issues);
        }

        public bool CanReach(Tile start, Tile destination)
        {
            if (start == null || destination == null)
            {
                return false;
            }

            if (!IsWalkable(start) || !IsWalkable(destination))
            {
                return false;
            }

            var visited = new HashSet<Tile>();
            var queue = new Queue<Tile>();
            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == destination)
                {
                    return true;
                }

                foreach (var next in GetNeighbors(current))
                {
                    if (next == null || visited.Contains(next) || !IsWalkable(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private void ValidateTiles(World world, List<WorldValidationIssue> issues)
        {
            var map = world.Map;
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var tile = map[x, y];
                    if (tile == null)
                    {
                        issues.Add(new WorldValidationIssue("tile.null", "Map tile is null.", x, y));
                        continue;
                    }

                    if (tile.X != x || tile.Y != y)
                    {
                        issues.Add(new WorldValidationIssue("tile.coordinate-mismatch", $"Tile reports {tile.X},{tile.Y}.", x, y));
                    }

                    if (tile.Terrain == null)
                    {
                        issues.Add(new WorldValidationIssue("tile.terrain-null", "Tile terrain is null.", x, y));
                    }
                    else if (!MapBuilder.TerrainKinds.ContainsKey(tile.Terrain.ShortName))
                    {
                        issues.Add(new WorldValidationIssue("tile.terrain-unknown", $"Unknown terrain '{tile.Terrain.ShortName}'.", x, y));
                    }
                }
            }
        }

        private void ValidateCities(World world, List<WorldValidationIssue> issues)
        {
            var occupied = new HashSet<Tile>();
            foreach (var city in world.GetCities())
            {
                if (city == null)
                {
                    issues.Add(new WorldValidationIssue("city.null", "World contains a null city."));
                    continue;
                }

                if (city.Tile == null)
                {
                    issues.Add(new WorldValidationIssue("city.tile-null", $"City '{city.ShortName}' has no tile."));
                    continue;
                }

                var x = city.Tile.X;
                var y = city.Tile.Y;
                if (!IsCityTopLeftInBounds(world, x, y))
                {
                    issues.Add(new WorldValidationIssue("city.out-of-bounds", $"City '{city.ShortName}' 2x2 footprint is outside the map.", x, y));
                    continue;
                }

                foreach (var tile in GetCityTiles(world, x, y))
                {
                    if (tile.City != city)
                    {
                        issues.Add(new WorldValidationIssue("city.footprint-broken", $"City '{city.ShortName}' footprint tile does not point back to the city.", tile.X, tile.Y));
                    }

                    if (!occupied.Add(tile))
                    {
                        issues.Add(new WorldValidationIssue("city.overlap", $"City '{city.ShortName}' overlaps another city.", tile.X, tile.Y));
                    }
                }
            }
        }

        private void ValidateLocations(World world, List<WorldValidationIssue> issues)
        {
            foreach (var location in world.GetLocations())
            {
                if (location == null)
                {
                    issues.Add(new WorldValidationIssue("location.null", "World contains a null location."));
                    continue;
                }

                if (location.Tile == null)
                {
                    issues.Add(new WorldValidationIssue("location.tile-null", $"Location '{location.ShortName}' has no tile."));
                    continue;
                }

                if (location.Tile.HasCity())
                {
                    issues.Add(new WorldValidationIssue("location.city-overlap", $"Location '{location.ShortName}' overlaps city '{location.Tile.City.ShortName}'.", location.X, location.Y));
                }
            }
        }

        private void ValidateStacks(World world, List<WorldValidationIssue> issues)
        {
            var map = world.Map;
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var tile = map[x, y];
                    if (tile == null)
                    {
                        continue;
                    }

                    var stationed = tile.Armies == null ? 0 : tile.Armies.Count;
                    var visiting = tile.VisitingArmies == null ? 0 : tile.VisitingArmies.Count;
                    if (stationed > Army.MaxArmies || visiting > Army.MaxArmies || stationed + visiting > Army.MaxArmies)
                    {
                        issues.Add(new WorldValidationIssue("stack.too-large", $"Stack has {stationed} stationed and {visiting} visiting armies; max is {Army.MaxArmies}.", x, y));
                    }
                }
            }
        }

        private void ValidatePlayers(IEnumerable<Player> players, List<WorldValidationIssue> issues)
        {
            if (players == null)
            {
                return;
            }

            foreach (var player in players)
            {
                if (player == null || player.IsDead)
                {
                    continue;
                }

                if (player.GetCities().Count == 0)
                {
                    issues.Add(new WorldValidationIssue("player.no-city", $"Active clan '{player?.Clan?.ShortName}' has no city."));
                }

                if (player.GetArmies().Count == 0)
                {
                    issues.Add(new WorldValidationIssue("player.no-army", $"Active clan '{player?.Clan?.ShortName}' has no army."));
                }

                foreach (var army in player.GetArmies())
                {
                    if (army.Tile == null)
                    {
                        issues.Add(new WorldValidationIssue("army.tile-null", $"Army '{army.ShortName}' has no tile."));
                    }
                    else if (!army.Tile.GetAllArmies().Contains(army))
                    {
                        issues.Add(new WorldValidationIssue("army.tile-mismatch", $"Army '{army.ShortName}' is not present on its tile.", army.X, army.Y));
                    }
                }
            }
        }

        private void ValidateReachability(World world, List<WorldValidationIssue> issues)
        {
            var anchors = world.GetCities()
                .Where(city => city != null && city.Tile != null)
                .Select(city => city.Tile)
                .Concat(world.GetLocations().Where(location => location != null && location.Tile != null).Select(location => location.Tile))
                .ToList();
            if (anchors.Count <= 1)
            {
                return;
            }

            var start = anchors[0];
            foreach (var destination in anchors.Skip(1))
            {
                if (!this.CanReach(start, destination))
                {
                    issues.Add(new WorldValidationIssue("reachability.unreachable", $"Map object at {destination.X},{destination.Y} is not reachable from {start.X},{start.Y}.", destination.X, destination.Y));
                }
            }
        }

        private static bool IsCityTopLeftInBounds(World world, int x, int y)
        {
            var map = world.Map;
            return x >= 0 && y >= 1 && x + 1 < map.GetLength(0) && y < map.GetLength(1);
        }

        private static IEnumerable<Tile> GetCityTiles(World world, int x, int y)
        {
            yield return world.Map[x, y];
            yield return world.Map[x, y - 1];
            yield return world.Map[x + 1, y];
            yield return world.Map[x + 1, y - 1];
        }

        private static IEnumerable<Tile> GetNeighbors(Tile tile)
        {
            var map = World.Current.Map;
            for (var x = tile.X - 1; x <= tile.X + 1; x++)
            {
                for (var y = tile.Y - 1; y <= tile.Y + 1; y++)
                {
                    if (x == tile.X && y == tile.Y)
                    {
                        continue;
                    }

                    if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
                    {
                        continue;
                    }

                    yield return map[x, y];
                }
            }
        }

        private static bool IsWalkable(Tile tile)
        {
            return tile != null && tile.Terrain != null && tile.Terrain.Info.AllowWalk;
        }
    }
}
