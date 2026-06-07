using System.Text.Json;
using Wism.Agent.Playground;
using Wism.Client.Modules.Profiles;

var command = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)) ?? "sample";
var quiet = args.Any(arg => string.Equals(arg, "--quiet", StringComparison.OrdinalIgnoreCase));
var profileId = ReadString(args, "profile", ModularGameProfileCatalog.DefaultProfileId);
var packIds = ReadCsv(args, "packs");
var selection = ModularGameProfileCatalog.Resolve(FindRepositoryRoot(), profileId, packIds);
var runner = new PlaygroundScenarioRunner(quiet, selection);
var channel = ReadString(args, "channel", null);

try
{
    switch (command.ToLowerInvariant())
    {
        case "sample":
            Print(runner.Sample(), quiet);
            return 0;
        case "win":
            return Exit(Print(runner.Win(), quiet));
        case "lose":
            return Exit(Print(runner.Lose(), quiet));
        case "parallel":
            var agents = ReadInt(args, "agents", 2);
            var reports = runner.ParallelSmoke(agents);
            Console.WriteLine(JsonSerializer.Serialize(reports, JsonOptions()));
            return reports.All(report => report.Status == "Passed") ? 0 : 1;
        case "companion":
            var scenario = ReadString(args, "scenario", "win") ?? "win";
            var delayMs = ReadInt(args, "delayMs", 300);
            return Exit(Print(runner.CompanionDemo(scenario, delayMs, channel), quiet));
        case "world":
            var world = ReadString(args, "world", selection.BaseWorld) ?? selection.BaseWorld;
            var modRoot = ReadString(args, "modRoot", null);
            return Exit(Print(runner.WorldSample(world, modRoot), quiet));
        case "record":
            var recordScenario = ReadString(args, "scenario", "win") ?? "win";
            var name = ReadString(args, "name", DefaultCaptureName(recordScenario)) ?? DefaultCaptureName(recordScenario);
            var outputRoot = ReadString(args, "out", DefaultCaptureOutputRoot()) ?? DefaultCaptureOutputRoot();
            var generateTest = ReadBool(args, "generateTest", true);
            var result = runner.Record(recordScenario, name, outputRoot, generateTest, channel);
            PrintCapture(result, quiet);
            return string.Equals(result.Status, "Passed", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        case "campaign":
            var campaignSeed = ReadInt(args, "seed", selection.Launch.Seed ?? 1990);
            var campaignClans = ReadInt(args, "clans", selection.Launch.Clans ?? 2);
            var maxTurns = ReadInt(args, "maxTurns", selection.Launch.MaxTurns ?? 40);
            var campaignOut = ReadString(args, "out", Path.Combine(FindRepositoryRoot(), "artifacts", "campaigns"));
            var campaignName = ReadString(args, "name", null);
            var campaignModRoot = ReadString(args, "modRoot", null);
            var campaignDelayMs = ReadInt(args, "delayMs", 0);
            var campaignSize = ReadString(args, "size", "medium") ?? "medium";
            var campaignScenario = ReadString(args, "scenario", ReadString(args, "preset", selection.Launch.Scenario ?? "standard")) ?? "standard";
            var campaign = runner.Campaign(campaignSeed, campaignClans, maxTurns, campaignOut, campaignName, campaignModRoot, campaignDelayMs, campaignSize, campaignScenario, channel);
            PrintCampaign(campaign, quiet);
            return string.Equals(campaign.Status, "Passed", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        case "eval":
            var evalSeed = ReadInt(args, "seed", 20260608);
            var evalCases = ReadInt(args, "cases", 50);
            var evalMaxTurns = ReadInt(args, "maxTurns", 12);
            var evalOut = ReadString(args, "out", Path.Combine(FindRepositoryRoot(), "artifacts", "evals"));
            var evalModRoot = ReadString(args, "modRoot", null);
            var evalScenarios = ReadCsv(args, "scenarios");
            var evalClans = ReadIntCsv(args, "clans", new[] { 2, 4 });
            var evalSizes = ReadCsv(args, "sizes");
            var eval = new EvalBatchRunner().Run(new EvalBatchOptions(
                Seed: evalSeed,
                Cases: evalCases,
                MaxTurns: evalMaxTurns,
                OutputRoot: evalOut ?? Path.Combine(FindRepositoryRoot(), "artifacts", "evals"),
                ScenarioFamilies: evalScenarios,
                ClanCounts: evalClans,
                Sizes: evalSizes,
                ModRoot: evalModRoot));
            PrintEval(eval, quiet);
            return string.Equals(eval.Status, "Passed", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        case "jump":
            var checkpoint = ReadString(args, "checkpoint", null);
            if (string.IsNullOrWhiteSpace(checkpoint))
            {
                Console.Error.WriteLine("checkpoint=<path> is required.");
                return 2;
            }

            return Exit(Print(runner.Jump(checkpoint), quiet));
        case "worktrees":
            var plan = PlaygroundScenarioRunner.CreateWorktreePlan(FindRepositoryRoot(), ReadInt(args, "agents", 4));
            Console.WriteLine(JsonSerializer.Serialize(plan, JsonOptions()));
            return 0;
        default:
            Console.WriteLine("Usage: Wism.Agent.Playground [sample|win|lose|parallel|companion|world|record|campaign|eval|jump|worktrees] [--quiet] [profile=classic-warlords] [packs=a,b] [agents=N] [scenario=win] [name=CapturedAsciiWin] [out=path] [generateTest=true] [delayMs=300] [channel=id] [world=TestWorld] [modRoot=path] [seed=1990] [clans=2..8] [maxTurns=40] [size=medium|large] [sizes=medium,large] [preset=standard|capture-pressure|ruin-search|classic-ai-production-vectoring] [scenarios=a,b] [cases=50] [checkpoint=path]");
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static PlaygroundReport Print(PlaygroundReport report, bool quiet)
{
    if (quiet)
    {
        Console.WriteLine($"{report.Scenario}:{report.Status}:{report.Outcome}");
        return report;
    }

    Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions()));
    return report;
}

static void PrintCapture(CaptureResult result, bool quiet)
{
    if (quiet)
    {
        Console.WriteLine($"{result.Name}:{result.Status}:{result.OutputDirectory}");
        return;
    }

    Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions()));
}

static void PrintCampaign(CampaignRunResult result, bool quiet)
{
    if (quiet)
    {
        Console.WriteLine($"{result.Name}:{result.Status}:{result.Outcome}:{result.OutputDirectory}");
        return;
    }

    Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions()));
}

static void PrintEval(EvalRunResult result, bool quiet)
{
    if (quiet)
    {
        Console.WriteLine($"{result.RunId}:{result.Status}:{result.Scorecard.PassedCases}/{result.Scorecard.TotalCases} cases:{result.OutputDirectory}");
        return;
    }

    Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions()));
}

static int Exit(PlaygroundReport report) =>
    string.Equals(report.Status, "Passed", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

static int ReadInt(IReadOnlyList<string> args, string name, int fallback)
{
    var prefix = name + "=";
    var value = args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    return value is not null && int.TryParse(value[prefix.Length..], out var parsed) ? parsed : fallback;
}

static string? ReadString(IReadOnlyList<string> args, string name, string? fallback)
{
    var prefix = name + "=";
    var value = args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    return value is not null ? value[prefix.Length..] : fallback;
}

static bool ReadBool(IReadOnlyList<string> args, string name, bool fallback)
{
    var value = ReadString(args, name, null);
    return value is null ? fallback : bool.TryParse(value, out var parsed) ? parsed : fallback;
}

static string[] ReadCsv(IReadOnlyList<string> args, string name)
{
    var value = ReadString(args, name, string.Empty);
    return string.IsNullOrWhiteSpace(value)
        ? Array.Empty<string>()
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static int[] ReadIntCsv(IReadOnlyList<string> args, string name, IReadOnlyList<int> fallback)
{
    var values = ReadCsv(args, name)
        .Select(value => int.TryParse(value, out var parsed) ? parsed : (int?)null)
        .Where(value => value.HasValue)
        .Select(value => value!.Value)
        .ToArray();
    return values.Length > 0 ? values : fallback.ToArray();
}

static string DefaultCaptureName(string scenario)
{
    if (string.IsNullOrWhiteSpace(scenario))
    {
        return "CapturedAsciiWin";
    }

    var suffix = string.Equals(scenario, "win", StringComparison.OrdinalIgnoreCase)
        ? "AsciiWin"
        : char.ToUpperInvariant(scenario[0]) + scenario[1..].ToLowerInvariant();
    return $"Captured{suffix}";
}

static string DefaultCaptureOutputRoot()
{
    return Path.Combine(
        FindRepositoryRoot(),
        "WismClient",
        "Wism.Client.Test",
        "AgentPlayground",
        "Captures");
}

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(Environment.CurrentDirectory);
    while (current is not null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
            Directory.Exists(Path.Combine(current.FullName, "WismClient")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return Environment.CurrentDirectory;
}

static JsonSerializerOptions JsonOptions() => new()
{
    WriteIndented = true
};
