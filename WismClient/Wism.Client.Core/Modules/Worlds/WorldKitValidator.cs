using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Modules.Worlds
{
    public static class WorldKitValidator
    {
        public static WorldKitValidationReport Validate(string repositoryRoot, string worldId, WorldKitValidationOptions options = null)
        {
            var modRoot = Profiles.ModularGameProfileCatalog.ResolveModRoot(repositoryRoot);
            return ValidateModRoot(modRoot, worldId, options);
        }

        public static WorldKitValidationReport ValidateModRoot(string modRoot, string worldId, WorldKitValidationOptions options = null)
        {
            options = options ?? new WorldKitValidationOptions();
            var report = new WorldKitValidationReport
            {
                WorldId = worldId ?? string.Empty,
                ModRoot = modRoot ?? string.Empty,
                WorldRoot = Path.Combine(modRoot ?? string.Empty, "Worlds", worldId ?? string.Empty)
            };
            report.Coverage.RequestedPlayers = options.RequestedPlayers;

            if (string.IsNullOrWhiteSpace(worldId))
            {
                report.Add(WorldKitValidationSeverity.Error, "world-id-missing", "A world id is required.", modRoot ?? string.Empty);
                return Finish(report);
            }

            if (!Directory.Exists(modRoot))
            {
                report.Add(WorldKitValidationSeverity.Error, "mod-root-missing", "Mod root was not found.", modRoot ?? string.Empty);
                return Finish(report);
            }

            if (!Directory.Exists(report.WorldRoot))
            {
                report.Add(WorldKitValidationSeverity.Error, "world-root-missing", $"World '{worldId}' was not found.", report.WorldRoot);
                return Finish(report);
            }

            var terrainIds = LoadStableIds<TerrainInfo>(report, Path.Combine(modRoot, "Terrain.json"), info => info.ShortName, "terrain-json-invalid");
            var terrainInfos = LoadInfos<TerrainInfo>(report, Path.Combine(modRoot, "Terrain.json"), "terrain-json-invalid")
                .Where(info => !string.IsNullOrWhiteSpace(info.ShortName))
                .GroupBy(info => info.ShortName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var clanIds = LoadStableIds<ClanInfo>(report, Path.Combine(modRoot, "Clan.json"), info => info.ShortName, "clan-json-invalid");
            var armyIds = LoadStableIds<ArmyInfo>(report, Path.Combine(modRoot, "Army.json"), info => info.ShortName, "army-json-invalid");

            var map = LoadMap(report, Path.Combine(report.WorldRoot, "Map.json"), terrainIds);
            var cities = LoadInfos<CityInfo>(report, Path.Combine(report.WorldRoot, "City.json"), "city-json-invalid");
            var locations = LoadLocationTokens(report, Path.Combine(report.WorldRoot, "Location.json"));

            ValidateCities(report, map, cities, clanIds, armyIds);
            ValidateLocations(report, map, locations);
            ValidateStarts(report, cities, clanIds, options);
            ValidateReachability(report, map, cities, terrainInfos);

            report.Coverage.Loadable = report.IsValid;
            return Finish(report);
        }

        static WorldKitValidationReport Finish(WorldKitValidationReport report)
        {
            report.ProofHints = new[]
            {
                $"dotnet run --project Wism.ModKit.Cli -- world validate world={report.WorldId} --json",
                $"Run a WismUnity read-only Mod Kit or world-builder status proof for world={report.WorldId}."
            };
            return report;
        }

        static MapData LoadMap(WorldKitValidationReport report, string mapPath, ISet<string> terrainIds)
        {
            var map = new MapData();
            if (!File.Exists(mapPath))
            {
                report.Add(WorldKitValidationSeverity.Error, "map-json-missing", "Map.json was not found.", mapPath);
                return map;
            }

            JArray tileArray;
            try
            {
                var token = JToken.Parse(File.ReadAllText(mapPath));
                tileArray = token.Type == JTokenType.Array ? (JArray)token : token["Tiles"] as JArray;
            }
            catch (JsonException ex)
            {
                report.Add(WorldKitValidationSeverity.Error, "map-json-invalid", ex.Message, mapPath);
                return map;
            }

            if (tileArray == null)
            {
                report.Add(WorldKitValidationSeverity.Error, "map-tiles-missing", "Map.json must contain a Tiles array.", mapPath);
                return map;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tileArray.OfType<JObject>())
            {
                var x = token["X"]?.Value<int>() ?? int.MinValue;
                var y = token["Y"]?.Value<int>() ?? int.MinValue;
                var terrain = token["TerrainShortName"]?.ToString() ?? string.Empty;
                if (x < 0 || y < 0)
                {
                    report.Add(WorldKitValidationSeverity.Error, "map-tile-coordinate-invalid", "Map tiles must have non-negative X and Y coordinates.", mapPath, token.Path, x == int.MinValue ? null : (int?)x, y == int.MinValue ? null : (int?)y);
                    continue;
                }

                var key = CoordinateKey(x, y);
                if (!seen.Add(key))
                {
                    report.Add(WorldKitValidationSeverity.Error, "map-tile-duplicate", $"Duplicate map tile at {x},{y}.", mapPath, token.Path, x, y);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(terrain))
                {
                    report.Add(WorldKitValidationSeverity.Error, "map-tile-terrain-missing", "Map tile is missing TerrainShortName.", mapPath, token.Path, x, y);
                }
                else if (!terrainIds.Contains(terrain))
                {
                    report.Add(WorldKitValidationSeverity.Error, "map-tile-terrain-unknown", $"Map tile references unknown terrain '{terrain}'.", mapPath, token.Path, x, y);
                }

                map.Tiles[key] = new MapTile(x, y, terrain);
                if (!map.TerrainCounts.ContainsKey(terrain))
                {
                    map.TerrainCounts[terrain] = 0;
                }

                map.TerrainCounts[terrain]++;
            }

            map.Width = map.Tiles.Count == 0 ? 0 : map.Tiles.Values.Max(tile => tile.X) + 1;
            map.Height = map.Tiles.Count == 0 ? 0 : map.Tiles.Values.Max(tile => tile.Y) + 1;
            report.Coverage.Width = map.Width;
            report.Coverage.Height = map.Height;
            report.Coverage.TileCount = map.Tiles.Count;
            report.Coverage.ExpectedTileCount = map.Width * map.Height;
            report.Coverage.TerrainCounts = new Dictionary<string, int>(map.TerrainCounts, StringComparer.OrdinalIgnoreCase);

            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    if (!map.Tiles.ContainsKey(CoordinateKey(x, y)))
                    {
                        report.Add(WorldKitValidationSeverity.Error, "map-tile-missing", $"Map is missing tile {x},{y}.", mapPath, "Tiles", x, y);
                    }
                }
            }

            return map;
        }

        static void ValidateCities(
            WorldKitValidationReport report,
            MapData map,
            IList<CityInfo> cities,
            ISet<string> clanIds,
            ISet<string> armyIds)
        {
            report.Coverage.CityCount = cities.Count;
            var occupied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var shortNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cityPath = Path.Combine(report.WorldRoot, "City.json");

            for (var i = 0; i < cities.Count; i++)
            {
                var city = cities[i];
                var jsonPath = $"[{i}]";
                if (string.IsNullOrWhiteSpace(city.ShortName))
                {
                    report.Add(WorldKitValidationSeverity.Error, "city-short-name-missing", "City is missing ShortName.", cityPath, jsonPath, city.X, city.Y);
                }
                else if (!shortNames.Add(city.ShortName))
                {
                    report.Add(WorldKitValidationSeverity.Error, "city-short-name-duplicate", $"Duplicate city ShortName '{city.ShortName}'.", cityPath, jsonPath, city.X, city.Y);
                }

                if (!string.Equals(city.ClanName, "Neutral", StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(city.ClanName) || !clanIds.Contains(city.ClanName)))
                {
                    report.Add(WorldKitValidationSeverity.Error, "city-owner-unknown", $"City '{city.ShortName}' references unknown clan '{city.ClanName}'.", cityPath, jsonPath, city.X, city.Y);
                }

                foreach (var point in CityFootprint(city.X, city.Y))
                {
                    var key = CoordinateKey(point.X, point.Y);
                    if (!map.Tiles.ContainsKey(key))
                    {
                        report.Add(WorldKitValidationSeverity.Error, "city-footprint-out-of-bounds", $"City '{city.ShortName}' footprint is outside map bounds.", cityPath, jsonPath, point.X, point.Y);
                        continue;
                    }

                    if (occupied.TryGetValue(key, out var otherCity))
                    {
                        report.Add(WorldKitValidationSeverity.Error, "city-footprint-overlap", $"City '{city.ShortName}' overlaps city '{otherCity}'.", cityPath, jsonPath, point.X, point.Y);
                    }
                    else
                    {
                        occupied[key] = city.ShortName;
                    }
                }

                foreach (var production in city.ProductionInfos ?? Array.Empty<ProductionInfo>())
                {
                    if (string.IsNullOrWhiteSpace(production.ArmyInfoName) || !armyIds.Contains(production.ArmyInfoName))
                    {
                        report.Add(WorldKitValidationSeverity.Error, "city-production-army-unknown", $"City '{city.ShortName}' references unknown production army '{production.ArmyInfoName}'.", cityPath, jsonPath, city.X, city.Y);
                    }
                }
            }
        }

        static void ValidateLocations(WorldKitValidationReport report, MapData map, IList<LocationRecord> locations)
        {
            report.Coverage.LocationCount = locations.Count;
            var shortNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var locationPath = Path.Combine(report.WorldRoot, "Location.json");

            foreach (var location in locations)
            {
                if (string.IsNullOrWhiteSpace(location.ShortName))
                {
                    report.Add(WorldKitValidationSeverity.Error, "location-short-name-missing", "Location is missing ShortName.", locationPath, location.JsonPath);
                }
                else if (!shortNames.Add(location.ShortName))
                {
                    report.Add(WorldKitValidationSeverity.Error, "location-short-name-duplicate", $"Duplicate location ShortName '{location.ShortName}'.", locationPath, location.JsonPath, location.X, location.Y);
                }

                if (!location.HasCoordinate)
                {
                    report.Add(WorldKitValidationSeverity.Warning, "location-coordinate-missing", $"Location '{location.ShortName}' is missing explicit X/Y coordinates and will load at legacy defaults.", locationPath, location.JsonPath);
                    continue;
                }

                if (!map.Tiles.ContainsKey(CoordinateKey(location.X, location.Y)))
                {
                    report.Add(WorldKitValidationSeverity.Error, "location-coordinate-out-of-bounds", $"Location '{location.ShortName}' is outside map bounds.", locationPath, location.JsonPath, location.X, location.Y);
                }
            }
        }

        static void ValidateStarts(
            WorldKitValidationReport report,
            IList<CityInfo> cities,
            ISet<string> clanIds,
            WorldKitValidationOptions options)
        {
            var starts = new HashSet<string>(
                cities
                    .Select(city => city.ClanName)
                    .Where(clan => !string.IsNullOrWhiteSpace(clan) && !string.Equals(clan, "Neutral", StringComparison.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
            report.Coverage.ClansWithStarts = starts.Count;

            foreach (var clan in starts)
            {
                if (!clanIds.Contains(clan))
                {
                    continue;
                }
            }

            foreach (var clan in options.ActiveClans ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(clan) || string.Equals(clan, "Neutral", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!clanIds.Contains(clan))
                {
                    report.Add(WorldKitValidationSeverity.Error, "start-clan-unknown", $"Requested start clan '{clan}' is not defined.", report.WorldRoot);
                }
                else if (!starts.Contains(clan))
                {
                    report.Add(WorldKitValidationSeverity.Error, "start-clan-missing-city", $"Requested clan '{clan}' has no owned starting city in world '{report.WorldId}'.", report.WorldRoot);
                }
            }

            if (options.RequestedPlayers > 0 && starts.Count < options.RequestedPlayers)
            {
                report.Add(WorldKitValidationSeverity.Error, "start-count-insufficient", $"World '{report.WorldId}' has {starts.Count} owned clan starts, but {options.RequestedPlayers} players were requested.", report.WorldRoot);
            }
        }

        static void ValidateReachability(
            WorldKitValidationReport report,
            MapData map,
            IList<CityInfo> cities,
            IDictionary<string, TerrainInfo> terrainInfos)
        {
            if (map.Tiles.Count == 0 || cities.Count < 2)
            {
                return;
            }

            var validCities = cities
                .Where(city => map.Tiles.ContainsKey(CoordinateKey(city.X, city.Y)))
                .ToArray();
            var totalPairs = validCities.Length * (validCities.Length - 1) / 2;
            var reachablePairs = 0;
            for (var i = 0; i < validCities.Length; i++)
            {
                var reachable = FindReachable(map, terrainInfos, validCities[i].X, validCities[i].Y);
                for (var j = i + 1; j < validCities.Length; j++)
                {
                    if (reachable.Contains(CoordinateKey(validCities[j].X, validCities[j].Y)))
                    {
                        reachablePairs++;
                    }
                }
            }

            report.Coverage.TotalCityPairs = totalPairs;
            report.Coverage.ReachableCityPairs = reachablePairs;
            if (totalPairs > 0 && reachablePairs == 0)
            {
                report.Add(WorldKitValidationSeverity.Warning, "city-reachability-none", "No city pairs are connected by generic walkable terrain. Naval or flying play may still be possible, but this world needs scenario-specific proof.", report.WorldRoot);
            }
        }

        static HashSet<string> FindReachable(
            MapData map,
            IDictionary<string, TerrainInfo> terrainInfos,
            int startX,
            int startY)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<MapPoint>();
            queue.Enqueue(new MapPoint(startX, startY));
            visited.Add(CoordinateKey(startX, startY));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in Neighbors(current.X, current.Y))
                {
                    var key = CoordinateKey(next.X, next.Y);
                    if (visited.Contains(key) || !map.Tiles.TryGetValue(key, out var tile))
                    {
                        continue;
                    }

                    if (!terrainInfos.TryGetValue(tile.Terrain, out var terrain) || !terrain.AllowWalk)
                    {
                        continue;
                    }

                    visited.Add(key);
                    queue.Enqueue(next);
                }
            }

            return visited;
        }

        static IList<T> LoadInfos<T>(WorldKitValidationReport report, string path, string code)
        {
            if (!File.Exists(path))
            {
                report.Add(WorldKitValidationSeverity.Error, code.Replace("invalid", "missing"), "Required world or mod JSON file was not found.", path);
                return new List<T>();
            }

            try
            {
                return JArray.Parse(File.ReadAllText(path)).ToObject<List<T>>() ?? new List<T>();
            }
            catch (JsonException ex)
            {
                report.Add(WorldKitValidationSeverity.Error, code, ex.Message, path);
                return new List<T>();
            }
        }

        static ISet<string> LoadStableIds<T>(WorldKitValidationReport report, string path, Func<T, string> selector, string code)
        {
            return new HashSet<string>(
                LoadInfos<T>(report, path, code)
                    .Select(selector)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
        }

        static IList<LocationRecord> LoadLocationTokens(WorldKitValidationReport report, string path)
        {
            if (!File.Exists(path))
            {
                report.Add(WorldKitValidationSeverity.Error, "location-json-missing", "Location.json was not found.", path);
                return new List<LocationRecord>();
            }

            try
            {
                return JArray.Parse(File.ReadAllText(path))
                    .OfType<JObject>()
                    .Select(token =>
                    {
                        var hasX = token["X"] != null;
                        var hasY = token["Y"] != null;
                        return new LocationRecord(
                            token["ShortName"]?.ToString() ?? string.Empty,
                            hasX ? token["X"].Value<int>() : 0,
                            hasY ? token["Y"].Value<int>() : 0,
                            hasX && hasY,
                            token.Path);
                    })
                    .ToArray();
            }
            catch (JsonException ex)
            {
                report.Add(WorldKitValidationSeverity.Error, "location-json-invalid", ex.Message, path);
                return new List<LocationRecord>();
            }
        }

        static IEnumerable<MapPoint> CityFootprint(int x, int y)
        {
            yield return new MapPoint(x, y);
            yield return new MapPoint(x, y - 1);
            yield return new MapPoint(x + 1, y);
            yield return new MapPoint(x + 1, y - 1);
        }

        static IEnumerable<MapPoint> Neighbors(int x, int y)
        {
            yield return new MapPoint(x - 1, y);
            yield return new MapPoint(x + 1, y);
            yield return new MapPoint(x, y - 1);
            yield return new MapPoint(x, y + 1);
        }

        static string CoordinateKey(int x, int y)
        {
            return x + "," + y;
        }

        sealed class MapData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public Dictionary<string, MapTile> Tiles { get; } = new Dictionary<string, MapTile>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> TerrainCounts { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        sealed class MapTile
        {
            public MapTile(int x, int y, string terrain)
            {
                X = x;
                Y = y;
                Terrain = terrain;
            }

            public int X { get; }
            public int Y { get; }
            public string Terrain { get; }
        }

        sealed class LocationRecord
        {
            public LocationRecord(string shortName, int x, int y, bool hasCoordinate, string jsonPath)
            {
                ShortName = shortName;
                X = x;
                Y = y;
                HasCoordinate = hasCoordinate;
                JsonPath = jsonPath ?? string.Empty;
            }

            public string ShortName { get; }
            public int X { get; }
            public int Y { get; }
            public bool HasCoordinate { get; }
            public string JsonPath { get; }
        }

        struct MapPoint
        {
            public MapPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
        }
    }
}
