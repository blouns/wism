using System.Text.Json;
using Wism.Agent.Playground;

var command = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)) ?? "sample";
var quiet = args.Any(arg => string.Equals(arg, "--quiet", StringComparison.OrdinalIgnoreCase));
var runner = new PlaygroundScenarioRunner();

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
        case "world":
            var world = ReadString(args, "world", "TestWorld") ?? "TestWorld";
            var modRoot = ReadString(args, "modRoot", null);
            return Exit(Print(runner.WorldSample(world, modRoot), quiet));
        case "worktrees":
            var plan = PlaygroundScenarioRunner.CreateWorktreePlan(FindRepositoryRoot(), ReadInt(args, "agents", 4));
            Console.WriteLine(JsonSerializer.Serialize(plan, JsonOptions()));
            return 0;
        default:
            Console.WriteLine("Usage: Wism.Agent.Playground [sample|win|lose|parallel|world|worktrees] [--quiet] [agents=N] [world=TestWorld] [modRoot=path]");
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
