using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Wism.ModKit.Cli;

public static class NearIlluriaKitBuilder
{
    const string DefaultSourceWorld = "Illuria";
    const string DefaultWorld = "Near-Illuria";
    const string DefaultProfile = "near-illuria";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static NearIlluriaKitBuildResult Build(NearIlluriaKitBuildOptions options)
    {
        options ??= new NearIlluriaKitBuildOptions();
        var repositoryRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(options.RepositoryRoot)
            ? Directory.GetCurrentDirectory()
            : options.RepositoryRoot);
        var sourceWorld = string.IsNullOrWhiteSpace(options.SourceWorld)
            ? DefaultSourceWorld
            : options.SourceWorld;
        var world = string.IsNullOrWhiteSpace(options.World)
            ? DefaultWorld
            : options.World;
        var profile = string.IsNullOrWhiteSpace(options.Profile)
            ? DefaultProfile
            : options.Profile;

        var sourceModRoot = Path.Combine(repositoryRoot, "WismClient", "Wism.Client.Core", "mod");
        if (!Directory.Exists(sourceModRoot))
        {
            sourceModRoot = Path.Combine(repositoryRoot, "mod");
        }

        var unityModRoot = string.IsNullOrWhiteSpace(options.UnityModRoot)
            ? Path.Combine(repositoryRoot, "WismUnity", "Assets", "Plugins", "WismClient", "Mods")
            : Path.GetFullPath(options.UnityModRoot);

        var sourceWorldRoot = Path.Combine(sourceModRoot, "Worlds", sourceWorld);
        var outputWorldRoot = Path.Combine(sourceModRoot, "Worlds", world);
        Directory.CreateDirectory(outputWorldRoot);

        var mapNode = ReadJsonNode(Path.Combine(sourceWorldRoot, "Map.json"));
        var map = LoadMap(mapNode);
        if (mapNode is JsonObject mapObject)
        {
            mapObject["Name"] = world;
        }

        var cityTemplates = ReadJsonArray(Path.Combine(sourceWorldRoot, "City.json"));
        var cities = PlaceCities(cityTemplates, map);
        var locations = PlaceLocations(ReadJsonArray(Path.Combine(sourceWorldRoot, "Location.json")), map, cities.CityFootprints);

        var written = new List<string>();
        written.Add(WriteJson(Path.Combine(outputWorldRoot, "Map.json"), mapNode));
        written.Add(WriteJson(Path.Combine(outputWorldRoot, "City.json"), cities.Array));
        written.Add(WriteJson(Path.Combine(outputWorldRoot, "Location.json"), locations.Array));
        written.Add(WriteJson(Path.Combine(sourceModRoot, "Profiles", profile, "profile.json"), BuildProfile(profile, world)));

        if (options.CopyToUnity && Directory.Exists(unityModRoot))
        {
            var unityWorldRoot = Path.Combine(unityModRoot, "Worlds", world);
            var unityProfileRoot = Path.Combine(unityModRoot, "Profiles", profile);
            Directory.CreateDirectory(unityWorldRoot);
            Directory.CreateDirectory(unityProfileRoot);
            foreach (var fileName in new[] { "Map.json", "City.json", "Location.json" })
            {
                var target = Path.Combine(unityWorldRoot, fileName);
                File.Copy(Path.Combine(outputWorldRoot, fileName), target, true);
                written.Add(target);
            }

            var unityProfile = Path.Combine(unityProfileRoot, "profile.json");
            File.Copy(Path.Combine(sourceModRoot, "Profiles", profile, "profile.json"), unityProfile, true);
            written.Add(unityProfile);

            EnsureUnityMeta(unityModRoot, unityWorldRoot);
            EnsureUnityMeta(unityModRoot, unityProfileRoot);
        }

        return new NearIlluriaKitBuildResult
        {
            SchemaVersion = 1,
            Status = "Passed",
            SourceWorld = sourceWorld,
            World = world,
            Profile = profile,
            Width = map.Width,
            Height = map.Height,
            CityCount = cities.Array.Count,
            LocationCount = locations.Array.Count,
            NavyCityCount = cities.NavyCityCount,
            AdjustedCityCount = cities.AdjustedCityCount,
            SourceModRoot = sourceModRoot,
            UnityModRoot = Directory.Exists(unityModRoot) ? unityModRoot : string.Empty,
            WrittenFiles = written.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    static JsonObject BuildProfile(string profile, string world)
    {
        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["id"] = profile,
            ["version"] = "1.0.0",
            ["minWismVersion"] = "0.1.0",
            ["displayName"] = "Near-Illuria",
            ["description"] = "A playable eight-clan world kit built from the Illuria map base with deterministic WISM city and site placement.",
            ["baseWorld"] = world,
            ["modeId"] = "classic",
            ["enabledPacks"] = new JsonArray("pack-dusklands-visual", "pack-illurian-legends-flavor"),
            ["modRoot"] = "mod",
            ["unityScene"] = "Assets/Scenes/Near-Illuria.unity",
            ["launch"] = new JsonObject
            {
                ["world"] = world,
                ["seed"] = 1990,
                ["clans"] = 8,
                ["maxTurns"] = 0,
                ["scenario"] = "standard"
            }
        };
    }

    static CityPlacementResult PlaceCities(JsonArray templates, MapData map)
    {
        var output = new JsonArray();
        var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var adjusted = 0;
        var navy = 0;

        foreach (var templateNode in templates)
        {
            var city = CloneObject(templateNode);
            var shortName = city["ShortName"]?.GetValue<string>() ?? string.Empty;
            var original = new Point(ReadInt(city, "X"), ReadInt(city, "Y"));
            var needsWater = HasNavyProduction(city);
            if (needsWater)
            {
                navy++;
            }

            var placed = FindCityAnchor(map, occupied, original, needsWater, shortName);
            if (placed.X != original.X || placed.Y != original.Y)
            {
                adjusted++;
            }

            city["X"] = placed.X;
            city["Y"] = placed.Y;
            foreach (var point in CityFootprint(placed))
            {
                occupied.Add(Key(point));
            }

            output.Add(city);
        }

        return new CityPlacementResult(output, occupied, adjusted, navy);
    }

    static LocationPlacementResult PlaceLocations(JsonArray templates, MapData map, ISet<string> cityFootprints)
    {
        var output = new JsonArray();
        var used = new HashSet<string>(cityFootprints, StringComparer.OrdinalIgnoreCase);
        foreach (var templateNode in templates)
        {
            var location = CloneObject(templateNode);
            var terrain = location["Terrain"]?.GetValue<string>() ?? location["Kind"]?.GetValue<string>() ?? "Ruins";
            var shortName = location["ShortName"]?.GetValue<string>() ?? string.Empty;
            var target = HashTarget(shortName, map.Width, map.Height);
            var point = FindLocationPoint(map, used, terrain, target);
            location["X"] = point.X;
            location["Y"] = point.Y;
            used.Add(Key(point));
            output.Add(location);
        }

        return new LocationPlacementResult(output);
    }

    static Point FindCityAnchor(
        MapData map,
        ISet<string> occupied,
        Point desired,
        bool needsWater,
        string shortName)
    {
        var candidates = new List<(Point Point, long Score)>();
        for (var y = 1; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width - 1; x++)
            {
                var point = new Point(x, y);
                if (!IsValidCityFootprint(map, occupied, point) ||
                    (needsWater && !HasAdjacentWater(map, point)))
                {
                    continue;
                }

                var score = DistanceScore(point, desired) * 100L + CityTerrainPenalty(map, point);
                score += TieBreak(shortName, point);
                candidates.Add((point, score));
            }
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"Could not place city '{shortName}'.");
        }

        return candidates.OrderBy(candidate => candidate.Score).First().Point;
    }

    static Point FindLocationPoint(MapData map, ISet<string> used, string terrain, Point target)
    {
        var exact = map.Tiles.Values
            .Where(tile => string.Equals(tile.Terrain, terrain, StringComparison.OrdinalIgnoreCase))
            .Where(tile => !used.Contains(Key(tile.Point)))
            .Select(tile => tile.Point)
            .ToArray();
        var candidates = exact.Length > 0
            ? exact
            : map.Tiles.Values
                .Where(tile => tile.IsWalkable && !used.Contains(Key(tile.Point)))
                .Select(tile => tile.Point)
                .ToArray();

        if (candidates.Length == 0)
        {
            throw new InvalidOperationException("Could not place location; no usable map tile was available.");
        }

        return candidates
            .OrderBy(point => DistanceScore(point, target))
            .ThenBy(point => point.Y)
            .ThenBy(point => point.X)
            .First();
    }

    static bool IsValidCityFootprint(MapData map, ISet<string> occupied, Point anchor)
    {
        foreach (var point in CityFootprint(anchor))
        {
            if (occupied.Contains(Key(point)) ||
                !map.Tiles.TryGetValue(Key(point), out var tile) ||
                !tile.IsBuildable)
            {
                return false;
            }
        }

        return true;
    }

    static bool HasAdjacentWater(MapData map, Point anchor)
    {
        foreach (var point in CityFootprint(anchor))
        {
            if (map.Tiles.TryGetValue(Key(point), out var tile) && tile.IsFloatable)
            {
                return true;
            }

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var neighbor = new Point(point.X + dx, point.Y + dy);
                    if (map.Tiles.TryGetValue(Key(neighbor), out var adjacent) && adjacent.IsFloatable)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    static long CityTerrainPenalty(MapData map, Point anchor)
    {
        var penalty = 0L;
        foreach (var point in CityFootprint(anchor))
        {
            var terrain = map.Tiles[Key(point)].Terrain;
            if (string.Equals(terrain, "Castle", StringComparison.OrdinalIgnoreCase))
            {
                penalty -= 50;
            }
            else if (string.Equals(terrain, "Grass", StringComparison.OrdinalIgnoreCase))
            {
                penalty += 0;
            }
            else if (string.Equals(terrain, "Road", StringComparison.OrdinalIgnoreCase))
            {
                penalty += 5;
            }
            else
            {
                penalty += 25;
            }
        }

        return penalty;
    }

    static bool HasNavyProduction(JsonObject city)
    {
        if (city["ProductionInfos"] is not JsonArray productionInfos)
        {
            return false;
        }

        return productionInfos
            .OfType<JsonObject>()
            .Any(info => string.Equals(info["ArmyInfoName"]?.GetValue<string>(), "Navy", StringComparison.OrdinalIgnoreCase));
    }

    static IEnumerable<Point> CityFootprint(Point anchor)
    {
        yield return anchor;
        yield return new Point(anchor.X, anchor.Y - 1);
        yield return new Point(anchor.X + 1, anchor.Y);
        yield return new Point(anchor.X + 1, anchor.Y - 1);
    }

    static MapData LoadMap(JsonNode node)
    {
        var tilesNode = node is JsonArray array
            ? array
            : node?["Tiles"] as JsonArray;
        if (tilesNode == null)
        {
            throw new InvalidDataException("Map.json must contain a Tiles array.");
        }

        var map = new MapData();
        foreach (var tileNode in tilesNode.OfType<JsonObject>())
        {
            var point = new Point(ReadInt(tileNode, "X"), ReadInt(tileNode, "Y"));
            var terrain = tileNode["TerrainShortName"]?.GetValue<string>() ?? string.Empty;
            map.Tiles[Key(point)] = new MapTile(point, terrain);
        }

        map.Width = map.Tiles.Count == 0 ? 0 : map.Tiles.Values.Max(tile => tile.Point.X) + 1;
        map.Height = map.Tiles.Count == 0 ? 0 : map.Tiles.Values.Max(tile => tile.Point.Y) + 1;
        return map;
    }

    static JsonNode ReadJsonNode(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Required Mod Kit file was not found.", path);
        }

        return JsonNode.Parse(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Could not parse JSON file {path}.");
    }

    static JsonArray ReadJsonArray(string path)
    {
        return ReadJsonNode(path) as JsonArray
            ?? throw new InvalidDataException($"{path} must contain a JSON array.");
    }

    static JsonObject CloneObject(JsonNode node)
    {
        return JsonNode.Parse(node.ToJsonString()) as JsonObject
            ?? throw new InvalidDataException("Expected a JSON object.");
    }

    static string WriteJson(string path, JsonNode node)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, node.ToJsonString(JsonOptions) + Environment.NewLine);
        return path;
    }

    static int ReadInt(JsonObject obj, string name)
    {
        return obj.TryGetPropertyValue(name, out var node) && node != null && node.GetValueKind() == JsonValueKind.Number
            ? node.GetValue<int>()
            : 0;
    }

    static long DistanceScore(Point left, Point right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return (long)dx * dx + (long)dy * dy;
    }

    static int TieBreak(string text, Point point)
    {
        var value = HashCode.Combine(text ?? string.Empty, point.X, point.Y);
        return Math.Abs(value % 17);
    }

    static Point HashTarget(string text, int width, int height)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        var x = BitConverter.ToUInt16(bytes, 0) % Math.Max(1, width);
        var y = BitConverter.ToUInt16(bytes, 2) % Math.Max(1, height);
        return new Point(x, y);
    }

    static string Key(Point point)
    {
        return point.X + "," + point.Y;
    }

    static void EnsureUnityMeta(string modRoot, string path)
    {
        var directories = new Stack<string>();
        var current = new DirectoryInfo(path);
        var root = new DirectoryInfo(modRoot);
        while (current != null && current.FullName.StartsWith(root.FullName, StringComparison.OrdinalIgnoreCase))
        {
            directories.Push(current.FullName);
            if (string.Equals(current.FullName, root.FullName, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = current.Parent;
        }

        foreach (var directory in directories)
        {
            WriteMeta(directory + ".meta", directory, true);
        }

        foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly))
        {
            WriteMeta(file + ".meta", file, false);
        }
    }

    static void WriteMeta(string metaPath, string assetPath, bool isFolder)
    {
        if (File.Exists(metaPath))
        {
            return;
        }

        var guid = GuidFor(assetPath);
        var contents = isFolder
            ? $"fileFormatVersion: 2\n" +
              $"guid: {guid}\n" +
              $"folderAsset: yes\n" +
              $"DefaultImporter:\n" +
              $"  externalObjects: {{}}\n" +
              $"  userData: \n" +
              $"  assetBundleName: \n" +
              $"  assetBundleVariant: \n"
            : $"fileFormatVersion: 2\n" +
              $"guid: {guid}\n" +
              $"TextScriptImporter:\n" +
              $"  externalObjects: {{}}\n" +
              $"  userData: \n" +
              $"  assetBundleName: \n" +
              $"  assetBundleVariant: \n";
        File.WriteAllText(metaPath, contents.Replace("\n", Environment.NewLine));
    }

    static string GuidFor(string path)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("wism-world-kit:" + path.Replace('\\', '/').ToLowerInvariant()));
        return Convert.ToHexString(hash).Substring(0, 32).ToLowerInvariant();
    }

    sealed class MapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Dictionary<string, MapTile> Tiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    sealed class MapTile
    {
        public MapTile(Point point, string terrain)
        {
            Point = point;
            Terrain = terrain ?? string.Empty;
        }

        public Point Point { get; }
        public string Terrain { get; }
        public bool IsFloatable => string.Equals(Terrain, "Water", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(Terrain, "Bridge", StringComparison.OrdinalIgnoreCase);
        public bool IsWalkable => !string.Equals(Terrain, "Water", StringComparison.OrdinalIgnoreCase) &&
                                  !string.Equals(Terrain, "Mountain", StringComparison.OrdinalIgnoreCase) &&
                                  !string.Equals(Terrain, "Void", StringComparison.OrdinalIgnoreCase);
        public bool IsBuildable => IsWalkable;
    }

    readonly record struct Point(int X, int Y);

    sealed record CityPlacementResult(JsonArray Array, HashSet<string> CityFootprints, int AdjustedCityCount, int NavyCityCount);

    sealed record LocationPlacementResult(JsonArray Array);
}

public sealed class NearIlluriaKitBuildOptions
{
    public string RepositoryRoot { get; set; } = string.Empty;
    public string UnityModRoot { get; set; } = string.Empty;
    public string SourceWorld { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;
    public string Profile { get; set; } = string.Empty;
    public bool CopyToUnity { get; set; } = true;
}

public sealed class NearIlluriaKitBuildResult
{
    public int SchemaVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceWorld { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;
    public string Profile { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int CityCount { get; set; }
    public int LocationCount { get; set; }
    public int NavyCityCount { get; set; }
    public int AdjustedCityCount { get; set; }
    public string SourceModRoot { get; set; } = string.Empty;
    public string UnityModRoot { get; set; } = string.Empty;
    public string[] WrittenFiles { get; set; } = Array.Empty<string>();
}
