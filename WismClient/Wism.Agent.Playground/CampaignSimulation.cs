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
    string Status,
    string Outcome,
    int Turns,
    string OutputDirectory,
    IReadOnlyList<string> Checkpoints,
    IReadOnlyList<string> Moments,
    PlaygroundReport FinalReport);

public sealed record CampaignMoment(
    string Kind,
    string Clan,
    int Turn,
    int CommandIndex,
    string CheckpointFile,
    string Context);

internal sealed record CampaignOptions(
    int Seed,
    int ClanCount,
    int MaxTurns,
    string Name,
    string OutputRoot,
    string? ModRoot);

internal sealed class CampaignScenarioBuilder
{
    private static readonly string[] ClanOrder =
    {
        "Sirians",
        "LordBane",
        "OrcsOfKor",
        "Elvallie"
    };

    private static readonly IReadOnlyDictionary<string, string> Capitals = new Dictionary<string, string>
    {
        ["Sirians"] = "Marthos",
        ["LordBane"] = "BanesCitadel",
        ["OrcsOfKor"] = "Kor",
        ["Elvallie"] = "Elvallie"
    };

    private static readonly string[] LocationOrder =
    {
        "TempleIris",
        "SeerRiver",
        "GoldenLibrary",
        "GreyportRuins"
    };

    public WorldValidationReport Build(CampaignOptions options)
    {
        var modRoot = ResolveModRoot(options.ModRoot);
        ModFactory.ModPath = modRoot;
        ModFactory.WorldsPath = "Worlds";
        ModFactory.WorldPath = "Illuria";
        MapBuilder.Initialize(modRoot, "Illuria");

        var clanCount = Math.Clamp(options.ClanCount, 2, 4);
        Game.CreateEmpty();
        Game.Current.RandomSeed = options.Seed;
        Game.Current.Random = new Random(options.Seed);
        Game.Current.WarStrategy = new DefaultWarStrategy();
        Game.Current.TraversalStrategy = CompositeTraversalStrategy.CreateDefault();
        Game.Current.MovementCoordinator = MovementStrategyCoordinator.CreateDefault();
        Game.Current.PathingStrategy = new DijkstraPathingStrategy();
        Game.Current.Players = ClanOrder.Take(clanCount)
            .Select(shortName => Player.Create(Clan.Create(ModFactory.FindClanInfo(shortName))))
            .ToList();
        Game.Current.Transition(GameState.Ready);

        var width = clanCount <= 2 ? 22 : 28;
        var height = clanCount <= 2 ? 16 : 20;
        var cityCoordinates = GetCityCoordinates(width, height, clanCount);
        var map = CreateMap(width, height, cityCoordinates, options.Seed);
        World.CreateWorld(map);
        World.Current.Name = $"GeneratedCampaign_{options.Seed}_{clanCount}";

        MapBuilder.AddCitiesFromInfos(World.Current, CreateCities(cityCoordinates, clanCount));
        MapBuilder.AddLocationsFromInfos(World.Current, CreateLocations(width, height, clanCount));
        MapBuilder.AllocateBoons(World.Current.GetLocations());
        AddStartingArmies();

        return new WorldValidator().Validate(World.Current, Game.Current.Players);
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

    private static IReadOnlyList<(int X, int Y)> GetCityCoordinates(int width, int height, int clanCount)
    {
        var coordinates = new List<(int X, int Y)>
        {
            (3, 4),
            (width - 5, height - 4),
            (width - 5, 4),
            (3, height - 4)
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

        map[x, y].Terrain = MapBuilder.TerrainKinds["Road"];
    }

    private static List<CityInfo> CreateCities(IReadOnlyList<(int X, int Y)> coordinates, int clanCount)
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

        return cities;
    }

    private static List<LocationInfo> CreateLocations(int width, int height, int clanCount)
    {
        var positions = new[]
        {
            (width / 2, height / 2),
            (width / 2 - 3, height / 2 + 2),
            (width / 2 + 3, height / 2 - 2),
            (width / 2, Math.Max(2, height / 2 - 4))
        };

        return LocationOrder.Take(clanCount).Select((shortName, index) => new LocationInfo
        {
            ShortName = shortName,
            X = positions[index].Item1,
            Y = positions[index].Item2
        }).ToList();
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
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), city.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), city.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), city.Tile);
        }
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
    private readonly List<string> checkpoints = new();
    private readonly List<CampaignMoment> moments = new();
    private int commandIndex;
    private bool prepared;

    public CampaignRecorder(CampaignOptions options)
    {
        this.outputDirectory = Path.Combine(options.OutputRoot, Sanitize(options.Name));
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
        this.PrepareOutputDirectory();
        var fileName = $"{this.checkpoints.Count:0000}-{kind}-turn{turn:000}-{Sanitize(clan)}.json";
        var path = Path.Combine(this.outputDirectory, fileName);
        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        File.WriteAllText(path, JsonConvert.SerializeObject(Game.Current.Snapshot(), settings));
        this.checkpoints.Add(path);
        this.moments.Add(new CampaignMoment(kind, clan, turn, this.commandIndex, fileName, context));
        return path;
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
