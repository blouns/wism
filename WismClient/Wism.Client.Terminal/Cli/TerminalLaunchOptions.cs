using Wism.Client.Terminal.Rendering;

namespace Wism.Client.Terminal.Cli;

public sealed class TerminalLaunchOptions
{
    public string ProfileId { get; init; } = "classic-warlords";

    public string[]? PackIds { get; init; }

    public string? World { get; init; }

    public string? ModRoot { get; init; }

    public int? ClanCount { get; init; }

    public int? Seed { get; init; }

    public bool Json { get; init; }

    public bool Agent { get; init; }

    public bool NoColor { get; init; }

    public bool NoAnimation { get; init; }

    public string? InputScript { get; init; }

    public string? OutputPath { get; init; }

    public string? RecordRoot { get; init; }

    public TileRenderMode TileMode { get; init; } = TileRenderMode.Readable;

    public static TerminalLaunchOptions From(TerminalCommand command)
    {
        return new TerminalLaunchOptions
        {
            ProfileId = command.GetString("profile", "classic-warlords")!,
            PackIds = command.Values.ContainsKey("packs") ? ReadCsv(command.GetString("packs")) : null,
            World = command.GetString("world"),
            ModRoot = command.GetString("modRoot"),
            ClanCount = command.Values.ContainsKey("clans") ? command.GetInt("clans", 8) : null,
            Seed = command.Values.ContainsKey("seed") ? command.GetInt("seed", 1990) : null,
            Json = command.HasFlag("json"),
            Agent = command.HasFlag("agent") || command.Values.ContainsKey("input"),
            NoColor = command.HasFlag("no-color"),
            NoAnimation = command.HasFlag("no-animation"),
            InputScript = command.GetString("input"),
            OutputPath = command.GetString("out"),
            RecordRoot = command.GetString("record", command.GetString("recordRoot")),
            TileMode = ParseTileMode(command.GetString("mode", command.GetString("tileMode", "readable")))
        };
    }

    private static string[] ReadCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static TileRenderMode ParseTileMode(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "compact" => TileRenderMode.Compact,
            "detailed" => TileRenderMode.Detailed,
            _ => TileRenderMode.Readable
        };
}
