using System.Diagnostics;
using Newtonsoft.Json;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Games;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.MovementStrategies;
using Wism.Client.Core.Armies.TerrainTraversalStrategies;
using Wism.Client.Core.Armies.WarStrategies;
using Wism.Client.Core.Validation;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;

namespace Wism.Agent.Playground;

public sealed record CampaignRunResult(
    int SchemaVersion,
    string Name,
    int Seed,
    int ClanCount,
    string AiProfile,
    string Status,
    string Outcome,
    int Turns,
    string OutputDirectory,
    IReadOnlyList<string> Checkpoints,
    IReadOnlyList<string> Moments,
    PlaygroundReport FinalReport,
    CampaignTelemetrySummary? Telemetry = null,
    VictoryOutcomeSnapshot? VictoryOutcome = null);

public sealed record CampaignTelemetrySummary(
    double RuntimeSeconds,
    double TimeoutBudgetSeconds,
    double TimeoutBudgetUsedPercent,
    int TurnsCompleted,
    double SecondsPerTurn,
    int CommandsExecuted,
    double CommandsPerTurn,
    int MeaningfulEvents,
    double MeaningfulEventsPerTurn,
    int MapWidth,
    int MapHeight,
    int TileCount,
    int FinalArmyCount,
    int FinalCityCount,
    IReadOnlyDictionary<string, int> CommandTypeCounts,
    IReadOnlyDictionary<string, CampaignTimingSummary> SystemTimings,
    string? TimeoutKind,
    string? LastMomentKind);

public sealed record CampaignMoment(
    string Kind,
    string Clan,
    int Turn,
    int CommandIndex,
    string CheckpointFile,
    string Context);

public sealed record CampaignTimingSummary(
    string Name,
    int Count,
    double TotalSeconds,
    double AverageSeconds,
    double MaxSeconds);

internal sealed class CampaignTimingAccumulator
{
    private readonly Dictionary<string, CampaignTimingStats> timings = new(StringComparer.OrdinalIgnoreCase);

    public void Clear() => this.timings.Clear();

    public void Record(string name, TimeSpan elapsed)
    {
        if (!this.timings.TryGetValue(name, out var stats))
        {
            stats = new CampaignTimingStats(name);
            this.timings[name] = stats;
        }

        stats.Add(elapsed);
    }

    public void Measure(string name, Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            action();
        }
        finally
        {
            stopwatch.Stop();
            this.Record(name, stopwatch.Elapsed);
        }
    }

    public IReadOnlyDictionary<string, CampaignTimingSummary> Snapshot() => this.timings
        .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(pair => pair.Key, pair => pair.Value.ToSummary(), StringComparer.OrdinalIgnoreCase);
}

internal sealed class CampaignTimingStats
{
    private TimeSpan total = TimeSpan.Zero;
    private TimeSpan max = TimeSpan.Zero;

    public CampaignTimingStats(string name) => this.Name = name;

    public string Name { get; }

    public int Count { get; private set; }

    public void Add(TimeSpan elapsed)
    {
        this.Count++;
        this.total += elapsed;
        if (elapsed > this.max)
        {
            this.max = elapsed;
        }
    }

    public CampaignTimingSummary ToSummary() => new(
        this.Name,
        this.Count,
        Math.Round(this.total.TotalSeconds, 3),
        this.Count <= 0 ? 0 : Math.Round(this.total.TotalSeconds / this.Count, 3),
        Math.Round(this.max.TotalSeconds, 3));
}

internal sealed record CampaignOptions(
    int Seed,
    int ClanCount,
    int MaxTurns,
    string Name,
    string OutputRoot,
    string? ModRoot,
    string Size,
    string ScenarioFamily,
    string AiProfile = "strategic",
    CampaignCheckpointMode CheckpointMode = CampaignCheckpointMode.Full,
    int WallClockTimeoutSeconds = 0);

internal enum CampaignCheckpointMode
{
    Full,
    Turns,
    Summary
}

internal sealed class CampaignScenarioBuilder
{
    private static readonly string[] ClanOrder =
    {
        "Sirians",
        "LordBane",
        "OrcsOfKor",
        "Elvallie",
        "StormGiants",
        "GreyDwarves",
        "Selentines",
        "HorseLords"
    };

    private static readonly IReadOnlyDictionary<string, string> Capitals = new Dictionary<string, string>
    {
        ["Sirians"] = "Marthos",
        ["LordBane"] = "BanesCitadel",
        ["OrcsOfKor"] = "Kor",
        ["Elvallie"] = "Elvallie",
        ["StormGiants"] = "Stormheim",
        ["GreyDwarves"] = "Khamar",
        ["Selentines"] = "Enmouth",
        ["HorseLords"] = "Dunethal"
    };

    private static readonly IReadOnlyDictionary<string, (int X, int Y)> MiniIlluriaCapitalAnchors = new Dictionary<string, (int X, int Y)>
    {
        ["Sirians"] = (52, 10),
        ["LordBane"] = (72, 57),
        ["OrcsOfKor"] = (75, 36),
        ["Elvallie"] = (36, 16),
        ["StormGiants"] = (16, 31),
        ["GreyDwarves"] = (25, 50),
        ["Selentines"] = (10, 62),
        ["HorseLords"] = (48, 47)
    };

    private static readonly string[] LocationOrder =
    {
        "TempleIris",
        "SeerRiver",
        "GoldenLibrary",
        "GreyportRuins"
    };

    private static readonly string[] SearchLocationOrder =
    {
        "GreyportRuins",
        "EmperorsCrypt",
        "TempleIris",
        "SeerRiver",
        "GoldenLibrary",
        "OldWoodsTower",
        "NirnsDeep",
        "MirkDecay"
    };

    private static readonly string[] OutpostOrder =
    {
        "Deserton",
        "Zaigonne",
        "Bereri",
        "Tal",
        "Minbourne",
        "Tirfing",
        "Amenal",
        "Pareth"
    };

    public WorldValidationReport Build(CampaignOptions options)
    {
        var modRoot = ResolveModRoot(options.ModRoot);
        ModFactory.ModPath = modRoot;
        ModFactory.WorldsPath = "Worlds";
        ModFactory.WorldPath = "Mini-Illuria";
        MapBuilder.Initialize(modRoot, "Mini-Illuria");

        var clanCount = Math.Clamp(options.ClanCount, 2, ClanOrder.Length);
        Game.CreateEmpty();
        Game.Current.RandomSeed = options.Seed;
        Game.Current.Random = new DeterministicRandom(options.Seed);
        Game.Current.WarStrategy = new DefaultWarStrategy();
        Game.Current.TraversalStrategy = CompositeTraversalStrategy.CreateDefault();
        Game.Current.MovementCoordinator = MovementStrategyCoordinator.CreateDefault();
        Game.Current.PathingStrategy = new DijkstraPathingStrategy();
        Game.Current.Players = ClanOrder.Take(clanCount)
            .Select(shortName => Player.Create(Clan.Create(ModFactory.FindClanInfo(shortName))))
            .ToList();
        Game.Current.Transition(GameState.Ready);

        var isLarge = string.Equals(options.Size, "large", StringComparison.OrdinalIgnoreCase);
        var width = isLarge ? 94 : clanCount <= 2 ? 22 : 28;
        var height = isLarge ? 80 : clanCount <= 2 ? 16 : 20;
        var scenarioFamily = NormalizeScenario(options.ScenarioFamily);
        var cityCoordinates = GetCityCoordinates(width, height, clanCount, isLarge);
        var outpostCoordinates = UsesCapturePressure(scenarioFamily) || UsesProductionEconomy(scenarioFamily)
            ? GetOutpostCoordinates(width, height, clanCount, isLarge)
            : Array.Empty<(int X, int Y)>();
        var allCityCoordinates = cityCoordinates.Concat(outpostCoordinates).ToArray();
        var locationCoordinates = GetLocationCoordinates(width, height, clanCount, isLarge, scenarioFamily, allCityCoordinates);
        var map = isLarge
            ? CreateWarlordsStyleMap(width, height, allCityCoordinates, locationCoordinates, options.Seed)
            : CreateMap(width, height, allCityCoordinates, options.Seed);
        World.CreateWorld(map);
        World.Current.Name = isLarge
            ? $"GeneratedMiniIlluriaLarge_{options.Seed}_{clanCount}"
            : $"GeneratedCampaign_{options.Seed}_{clanCount}";

        MapBuilder.AddCitiesFromInfos(World.Current, CreateCities(cityCoordinates, clanCount, outpostCoordinates, scenarioFamily));
        MapBuilder.AddLocationsFromInfos(World.Current, CreateLocations(locationCoordinates, clanCount, scenarioFamily));
        MapBuilder.AllocateBoons(World.Current.GetLocations());
        AddStartingArmies();
        AddScenarioProbeArmies(scenarioFamily, clanCount);

        return new WorldValidator().Validate(World.Current, Game.Current.Players);
    }

    private static Tile[,] CreateWarlordsStyleMap(
        int width,
        int height,
        IReadOnlyList<(int X, int Y)> cityCoordinates,
        IReadOnlyList<(int X, int Y)> locationCoordinates,
        int seed)
    {
        var random = new Random(seed);
        var map = new Tile[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var terrain = "Grass";
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    terrain = "Water";
                }

                map[x, y] = new Tile { Terrain = MapBuilder.TerrainKinds[terrain] };
            }
        }

        MapBuilder.AffixMapObjects(map);

        PaintRiver(map, width * 2 / 3, 4, height - 5, seed);
        PaintLake(map, width / 5, height / 3, 5, 4);
        PaintLake(map, width * 3 / 4, height * 2 / 3, 7, 5);
        PaintMountainRange(map, width / 3, height - 8, 18, descending: false);
        PaintMountainRange(map, width * 3 / 4, height / 2, 26, descending: true);
        PaintPatch(map, random, "Forest", width / 6, height * 3 / 4, 14, 9);
        PaintPatch(map, random, "Forest", width * 4 / 5, height / 4, 15, 11);
        PaintPatch(map, random, "Marsh", width / 5, height / 3 - 2, 13, 7);
        PaintPatch(map, random, "Hill", width / 2, height / 2, 18, 9);

        foreach (var city in cityCoordinates)
        {
            ClearLandAround(map, city.X, city.Y, radius: 3);
        }

        foreach (var location in locationCoordinates)
        {
            ClearLandAround(map, location.X, location.Y, radius: 2);
        }

        for (var i = 1; i < cityCoordinates.Count; i++)
        {
            CarveRoad(map, cityCoordinates[0], cityCoordinates[i]);
        }

        foreach (var location in locationCoordinates)
        {
            CarveRoad(map, cityCoordinates[0], location);
        }

        CarveRoad(map, (width / 5, height / 3 + 3), (width * 4 / 5, height / 4));
        CarveRoad(map, (width / 3, height - 9), (width * 3 / 4, height / 2));

        return map;
    }

    private static Tile[,] CreateMap(int width, int height, IReadOnlyList<(int X, int Y)> cityCoordinates, int seed)
    {
        var random = new Random(seed);
        var map = new Tile[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var terrain = "Grass";
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    terrain = "Mountain";
                }
                else
                {
                    var roll = random.Next(100);
                    if (roll < 8)
                    {
                        terrain = "Forest";
                    }
                    else if (roll < 12)
                    {
                        terrain = "Hill";
                    }
                    else if (roll < 15)
                    {
                        terrain = "Marsh";
                    }
                }

                map[x, y] = new Tile
                {
                    Terrain = MapBuilder.TerrainKinds[terrain]
                };
            }
        }

        MapBuilder.AffixMapObjects(map);
        for (var i = 1; i < cityCoordinates.Count; i++)
        {
            CarveRoad(map, cityCoordinates[0], cityCoordinates[i]);
        }

        return map;
    }

    private static IReadOnlyList<(int X, int Y)> GetCityCoordinates(int width, int height, int clanCount, bool isLarge)
    {
        if (isLarge)
        {
            return ClanOrder
                .Take(clanCount)
                .Select(clan => MiniIlluriaCapitalAnchors[clan])
                .ToArray();
        }

        var coordinates = new List<(int X, int Y)>
        {
            (3, 4),
            (width - 5, height - 4),
            (width - 5, 4),
            (3, height - 4),
            (width / 2, 3),
            (width / 2, height - 4),
            (width / 4, height / 2),
            (width * 3 / 4, height / 2)
        };

        return coordinates.Take(clanCount).ToArray();
    }

    private static void CarveRoad(Tile[,] map, (int X, int Y) start, (int X, int Y) end)
    {
        var x = start.X;
        var y = start.Y;
        while (x != end.X)
        {
            SetRoad(map, x, y);
            x += x < end.X ? 1 : -1;
        }

        while (y != end.Y)
        {
            SetRoad(map, x, y);
            y += y < end.Y ? 1 : -1;
        }

        SetRoad(map, x, y);
    }

    private static void SetRoad(Tile[,] map, int x, int y)
    {
        if (x <= 0 || y <= 0 || x >= map.GetLength(0) - 1 || y >= map.GetLength(1) - 1)
        {
            return;
        }

        map[x, y].Terrain = map[x, y].Terrain.ShortName == "Water"
            ? MapBuilder.TerrainKinds["Bridge"]
            : MapBuilder.TerrainKinds["Road"];
    }

    private static List<CityInfo> CreateCities(
        IReadOnlyList<(int X, int Y)> coordinates,
        int clanCount,
        IReadOnlyList<(int X, int Y)> outpostCoordinates,
        string scenarioFamily)
    {
        var cities = new List<CityInfo>();
        for (var i = 0; i < clanCount; i++)
        {
            var clanInfo = ModFactory.FindClanInfo(ClanOrder[i]);
            cities.Add(new CityInfo
            {
                ShortName = Capitals[clanInfo.ShortName],
                DisplayName = Capitals[clanInfo.ShortName],
                ClanName = clanInfo.ShortName,
                Defense = 5,
                Income = 30,
                X = coordinates[i].X,
                Y = coordinates[i].Y,
                ProductionInfos = DefaultProduction()
            });
        }

        for (var i = 0; i < Math.Min(clanCount, outpostCoordinates.Count); i++)
        {
            var ownerIndex = UsesProductionEconomy(scenarioFamily) ? i : (i + 1) % clanCount;
            var clanInfo = ModFactory.FindClanInfo(ClanOrder[ownerIndex]);
            cities.Add(new CityInfo
            {
                ShortName = OutpostOrder[i],
                DisplayName = OutpostOrder[i],
                ClanName = clanInfo.ShortName,
                Defense = 4,
                Income = 20,
                X = outpostCoordinates[i].X,
                Y = outpostCoordinates[i].Y,
                ProductionInfos = DefaultProduction()
            });
        }

        return cities;
    }

    private static IReadOnlyList<(int X, int Y)> GetOutpostCoordinates(int width, int height, int clanCount, bool isLarge)
    {
        if (isLarge)
        {
            return ClanOrder
                .Take(clanCount)
                .Select(clan => StepToward(MiniIlluriaCapitalAnchors[clan], (width / 2, height / 2), 7))
                .ToArray();
        }

        var positions = clanCount <= 2
            ? new[] { (8, 4), (width - 9, height - 4) }
            : new[]
            {
                (8, 4),
                (width - 9, height - 4),
                (width - 9, 4),
                (8, height - 4),
                (width / 2 - 4, 7),
                (width / 2 + 4, height - 8),
                (width / 4 + 4, height / 2),
                (width * 3 / 4 - 4, height / 2)
            };

        return positions.Take(clanCount).ToArray();
    }

    private static IReadOnlyList<(int X, int Y)> GetLocationCoordinates(
        int width,
        int height,
        int clanCount,
        bool isLarge,
        string scenarioFamily,
        IReadOnlyList<(int X, int Y)> cityCoordinates)
    {
        if (UsesRuinSearch(scenarioFamily))
        {
            var searchPositions = cityCoordinates
                .Take(clanCount)
                .Select(city => ClampInside(StepToward(city, (width / 2, height / 2), isLarge ? 6 : 3), width, height))
                .ToArray();
            return AvoidCityFootprints(searchPositions, cityCoordinates, width, height);
        }

        var positions = isLarge
            ? new[]
            {
                (width / 2, height / 2),
                (width / 4, height * 3 / 4),
                (width * 3 / 4, height / 4),
                (width * 4 / 5, height * 2 / 3)
            }
            : new[]
        {
            (width / 2, height / 2),
            (width / 2 - 3, height / 2 + 2),
            (width / 2 + 3, height / 2 - 2),
            (width / 2, Math.Max(2, height / 2 - 4))
        };

        return AvoidCityFootprints(positions.Take(clanCount).ToArray(), cityCoordinates, width, height);
    }

    private static List<LocationInfo> CreateLocations(IReadOnlyList<(int X, int Y)> positions, int clanCount, string scenarioFamily)
    {
        var order = UsesRuinSearch(scenarioFamily) ? SearchLocationOrder : LocationOrder;
        return order.Take(clanCount).Select((shortName, index) => new LocationInfo
        {
            ShortName = shortName,
            X = positions[index].X,
            Y = positions[index].Y
        }).ToList();
    }

    private static void PaintRiver(Tile[,] map, int baseX, int yStart, int yEnd, int seed)
    {
        for (var y = yStart; y <= yEnd; y++)
        {
            var bend = (int)Math.Round(Math.Sin((y + seed % 13) / 5.0) * 4);
            for (var dx = -1; dx <= 1; dx++)
            {
                SetTerrain(map, baseX + bend + dx, y, "Water");
            }
        }
    }

    private static void PaintLake(Tile[,] map, int centerX, int centerY, int radiusX, int radiusY)
    {
        for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
        {
            for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                var nx = (x - centerX) / (double)radiusX;
                var ny = (y - centerY) / (double)radiusY;
                if (nx * nx + ny * ny <= 1.0)
                {
                    SetTerrain(map, x, y, "Water");
                }
            }
        }
    }

    private static void PaintMountainRange(Tile[,] map, int startX, int startY, int length, bool descending)
    {
        for (var i = 0; i < length; i++)
        {
            var x = startX + i;
            var y = descending ? startY - i / 2 : startY + (int)Math.Sin(i / 2.0) * 2;
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) <= 2)
                    {
                        SetTerrain(map, x + dx, y + dy, "Mountain");
                    }
                }
            }
        }
    }

    private static void PaintPatch(Tile[,] map, Random random, string terrain, int centerX, int centerY, int width, int height)
    {
        for (var x = centerX - width / 2; x <= centerX + width / 2; x++)
        {
            for (var y = centerY - height / 2; y <= centerY + height / 2; y++)
            {
                var dx = Math.Abs(x - centerX) / (double)Math.Max(1, width / 2);
                var dy = Math.Abs(y - centerY) / (double)Math.Max(1, height / 2);
                if (dx * dx + dy * dy <= 1.15 && random.Next(100) < 75)
                {
                    SetTerrain(map, x, y, terrain);
                }
            }
        }
    }

    private static void ClearLandAround(Tile[,] map, int centerX, int centerY, int radius)
    {
        for (var x = centerX - radius; x <= centerX + radius; x++)
        {
            for (var y = centerY - radius; y <= centerY + radius; y++)
            {
                SetTerrain(map, x, y, "Grass");
            }
        }
    }

    private static void SetTerrain(Tile[,] map, int x, int y, string terrain)
    {
        if (x <= 0 || y <= 0 || x >= map.GetLength(0) - 1 || y >= map.GetLength(1) - 1)
        {
            return;
        }

        map[x, y].Terrain = MapBuilder.TerrainKinds[terrain];
    }

    private static ProductionInfo[] DefaultProduction()
    {
        return new[]
        {
            new ProductionInfo { ArmyInfoName = "LightInfantry", Moves = 10, Strength = 3, TurnsToProduce = 1, Upkeep = 4 },
            new ProductionInfo { ArmyInfoName = "HeavyInfantry", Moves = 8, Strength = 5, TurnsToProduce = 2, Upkeep = 4 },
            new ProductionInfo { ArmyInfoName = "Cavalry", Moves = 16, Strength = 6, TurnsToProduce = 4, Upkeep = 8 }
        };
    }

    private static void AddStartingArmies()
    {
        foreach (var player in Game.Current.Players)
        {
            player.Gold = 400;
            var city = player.Capitol;
            player.HireHero(city.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), city.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), city.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), city.Tile);
        }
    }

    private static void AddScenarioProbeArmies(string scenarioFamily, int clanCount)
    {
        AddModuleTensionProbeArmies(scenarioFamily);

        if (!scenarioFamily.Contains("siege", StringComparison.OrdinalIgnoreCase) &&
            !scenarioFamily.Contains("lost-battle", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var defenderName = scenarioFamily.Contains("lost-battle", StringComparison.OrdinalIgnoreCase)
            ? "HeavyInfantry"
            : "LightInfantry";
        foreach (var outpostName in OutpostOrder.Take(Math.Min(clanCount, OutpostOrder.Length)))
        {
            var city = World.Current.FindCity(outpostName);
            if (city?.Clan?.Player == null)
            {
                continue;
            }

            city.Clan.Player.ConscriptArmy(ArmyInfo.GetArmyInfo(defenderName), city.Tile);
        }
    }

    private static void AddModuleTensionProbeArmies(string scenarioFamily)
    {
        if (!UsesModuleTension(scenarioFamily) || Game.Current.Players.Count < 2)
        {
            return;
        }

        if (scenarioFamily.Contains("blocked-search", StringComparison.OrdinalIgnoreCase))
        {
            AddBlockedSearchProbe();
        }

        if (scenarioFamily.Contains("contested-siege", StringComparison.OrdinalIgnoreCase))
        {
            AddContestedSiegeProbe();
        }
    }

    private static void AddBlockedSearchProbe()
    {
        var location = World.Current.GetLocations().FirstOrDefault();
        if (location == null)
        {
            return;
        }

        var attacker = Game.Current.Players[0];
        var blocker = Game.Current.Players[1];
        var stagingTile = FindAdjacentOpenTile(location.Tile, attacker);
        if (stagingTile == null)
        {
            return;
        }

        attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), stagingTile);
        blocker.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), location.Tile);
    }

    private static void AddContestedSiegeProbe()
    {
        var outpost = OutpostOrder
            .Select(name => World.Current.FindCity(name))
            .FirstOrDefault(city => city?.Clan?.Player != null && city.Clan.Player != Game.Current.Players[0]);
        if (outpost == null)
        {
            return;
        }

        var attacker = Game.Current.Players[0];
        var defender = outpost.Clan.Player;
        var stagingTile = FindAdjacentOpenTile(outpost.Tile, attacker);
        if (stagingTile == null)
        {
            return;
        }

        attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), stagingTile);
        defender.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), outpost.Tile);
    }

    private static Tile? FindAdjacentOpenTile(Tile target, Player owner)
    {
        var probeInfo = ArmyInfo.GetArmyInfo("HeavyInfantry");
        var candidates = new[]
        {
            (target.X - 1, target.Y),
            (target.X + 1, target.Y),
            (target.X, target.Y - 1),
            (target.X, target.Y + 1)
        };

        foreach (var (x, y) in candidates)
        {
            if (x <= 0 || y <= 0 || x >= World.Current.Map.GetLength(0) - 1 || y >= World.Current.Map.GetLength(1) - 1)
            {
                continue;
            }

            var tile = World.Current.Map[x, y];
            if (!Game.Current.TraversalStrategy.CanTraverse(owner.Clan, probeInfo, tile))
            {
                continue;
            }

            if (tile.MusterArmy().Any(army => army.Clan != owner.Clan))
            {
                continue;
            }

            return tile;
        }

        return null;
    }

    private static (int X, int Y) StepToward((int X, int Y) start, (int X, int Y) target, int steps)
    {
        var x = start.X;
        var y = start.Y;
        for (var i = 0; i < steps; i++)
        {
            if (Math.Abs(target.X - x) >= Math.Abs(target.Y - y) && x != target.X)
            {
                x += x < target.X ? 1 : -1;
            }
            else if (y != target.Y)
            {
                y += y < target.Y ? 1 : -1;
            }
        }

        return (x, y);
    }

    private static (int X, int Y) ClampInside((int X, int Y) point, int width, int height)
    {
        return (Math.Clamp(point.X, 2, width - 3), Math.Clamp(point.Y, 2, height - 3));
    }

    private static IReadOnlyList<(int X, int Y)> AvoidCityFootprints(
        IReadOnlyList<(int X, int Y)> positions,
        IReadOnlyList<(int X, int Y)> cityCoordinates,
        int width,
        int height)
    {
        var blocked = new HashSet<(int X, int Y)>();
        foreach (var city in cityCoordinates)
        {
            blocked.Add((city.X, city.Y));
            blocked.Add((city.X, city.Y - 1));
            blocked.Add((city.X + 1, city.Y));
            blocked.Add((city.X + 1, city.Y - 1));
        }

        var resolved = new List<(int X, int Y)>(positions.Count);
        var used = new HashSet<(int X, int Y)>();
        foreach (var position in positions)
        {
            var candidate = ClampInside(position, width, height);
            if (blocked.Contains(candidate) || used.Contains(candidate))
            {
                candidate = FindNearestOpenLocationTile(candidate, blocked, used, width, height);
            }

            resolved.Add(candidate);
            used.Add(candidate);
        }

        return resolved;
    }

    private static (int X, int Y) FindNearestOpenLocationTile(
        (int X, int Y) start,
        HashSet<(int X, int Y)> blocked,
        HashSet<(int X, int Y)> used,
        int width,
        int height)
    {
        for (var radius = 1; radius < Math.Max(width, height); radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) != radius)
                    {
                        continue;
                    }

                    var candidate = ClampInside((start.X + dx, start.Y + dy), width, height);
                    if (!blocked.Contains(candidate) && !used.Contains(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return start;
    }

    private static string NormalizeScenario(string scenarioFamily)
    {
        return string.IsNullOrWhiteSpace(scenarioFamily)
            ? "standard"
            : scenarioFamily.Trim().ToLowerInvariant();
    }

    private static bool UsesCapturePressure(string scenarioFamily)
    {
        return scenarioFamily.Contains("capture", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("empty-city", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("expansion", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("conquest", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("contact", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("recovery", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("siege", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("pressure", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesProductionEconomy(string scenarioFamily)
    {
        return scenarioFamily.Contains("production", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("economy", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesRuinSearch(string scenarioFamily)
    {
        return scenarioFamily.Contains("search", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("ruin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesModuleTension(string scenarioFamily)
    {
        return scenarioFamily.Contains("tension", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("blocked-search", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("contested-siege", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveModRoot(string? requestedModRoot)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestedModRoot))
        {
            candidates.Add(requestedModRoot);
        }

        candidates.AddRange(new[]
        {
            Path.Combine(AppContext.BaseDirectory, "mod"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "WismClient", "Wism.Client.Core", "mod"))
        });

        var modRoot = candidates.FirstOrDefault(path =>
            File.Exists(Path.Combine(path, "Clan.json")) &&
            File.Exists(Path.Combine(path, "Worlds", "Illuria", "City.json")) &&
            File.Exists(Path.Combine(path, "Worlds", "Illuria", "Location.json")));
        return modRoot ?? throw new DirectoryNotFoundException("Could not find WISM mod files for campaign generation.");
    }
}

internal sealed class CampaignRecorder
{
    private readonly string outputDirectory;
    private readonly CampaignCheckpointMode checkpointMode;
    private readonly CampaignTimingAccumulator? timings;
    private readonly List<string> checkpoints = new();
    private readonly List<CampaignMoment> moments = new();
    private int commandIndex;
    private bool prepared;

    public CampaignRecorder(CampaignOptions options, CampaignTimingAccumulator? timings = null)
    {
        this.outputDirectory = Path.Combine(options.OutputRoot, Sanitize(options.Name));
        this.checkpointMode = options.CheckpointMode;
        this.timings = timings;
    }

    public string OutputDirectory => this.outputDirectory;

    public IReadOnlyList<string> Checkpoints => this.checkpoints;

    public IReadOnlyList<CampaignMoment> Moments => this.moments;

    public int CommandIndex => this.commandIndex;

    public void CountCommand()
    {
        this.commandIndex++;
    }

    public string Checkpoint(string kind, int turn, string clan, string context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            this.PrepareOutputDirectory();
            var fileName = string.Empty;
            if (this.ShouldWriteSnapshot(kind))
            {
                fileName = $"{this.checkpoints.Count:0000}-{kind}-turn{turn:000}-{Sanitize(clan)}.json";
                var path = Path.Combine(this.outputDirectory, fileName);
                var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
                File.WriteAllText(path, JsonConvert.SerializeObject(Game.Current.Snapshot(), settings));
                this.checkpoints.Add(path);
            }

            var moment = new CampaignMoment(kind, clan, turn, this.commandIndex, fileName, context);
            this.moments.Add(moment);
            File.AppendAllText(
                Path.Combine(this.outputDirectory, "checkpoint-index.jsonl"),
                JsonConvert.SerializeObject(moment) + Environment.NewLine);
            return fileName.Length == 0 ? string.Empty : Path.Combine(this.outputDirectory, fileName);
        }
        finally
        {
            stopwatch.Stop();
            this.timings?.Record("checkpoint-load", stopwatch.Elapsed);
        }
    }
    private bool ShouldWriteSnapshot(string kind)
    {
        if (this.checkpointMode == CampaignCheckpointMode.Full)
        {
            return true;
        }

        if (kind.Equals("command-timeout", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("invalid", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("setup", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("mission-complete", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("dominance-victory", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("victory", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("stalemate", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (this.checkpointMode == CampaignCheckpointMode.Turns)
        {
            return kind.Equals("turn-end", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public void SaveManifest(CampaignRunResult result)
    {
        this.PrepareOutputDirectory();
        var manifest = Path.Combine(this.outputDirectory, "campaign.json");
        File.WriteAllText(manifest, System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private void PrepareOutputDirectory()
    {
        if (this.prepared)
        {
            return;
        }

        if (Directory.Exists(this.outputDirectory))
        {
            Directory.Delete(this.outputDirectory, recursive: true);
        }

        Directory.CreateDirectory(this.outputDirectory);
        this.prepared = true;
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_').ToArray();
        var sanitized = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "campaign" : sanitized;
    }
}
