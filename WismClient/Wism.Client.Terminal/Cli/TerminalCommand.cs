namespace Wism.Client.Terminal.Cli;

public sealed class TerminalCommand
{
    private TerminalCommand(
        string name,
        IReadOnlyList<string> positionals,
        IReadOnlyDictionary<string, string> values,
        IReadOnlySet<string> flags)
    {
        Name = name;
        Positionals = positionals;
        Values = values;
        Flags = flags;
    }

    public string Name { get; }

    public IReadOnlyList<string> Positionals { get; }

    public IReadOnlyDictionary<string, string> Values { get; }

    public IReadOnlySet<string> Flags { get; }

    public bool HasFlag(string name) => Flags.Contains(name);

    public string? GetString(string name, string? fallback = null) =>
        Values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    public int GetInt(string name, int fallback)
    {
        var value = GetString(name);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public static TerminalCommand Parse(IReadOnlyList<string> args)
    {
        var positionals = new List<string>();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var flag = arg[2..];
                var split = flag.Split(new[] { '=' }, 2);
                if (split.Length == 2)
                {
                    values[split[0]] = split[1];
                }
                else
                {
                    flags.Add(flag);
                }

                continue;
            }

            var parts = arg.Split(new[] { '=' }, 2);
            if (parts.Length == 2)
            {
                values[parts[0]] = parts[1];
            }
            else
            {
                positionals.Add(arg);
            }
        }

        var name = positionals.Count == 0 ? "play" : positionals[0].ToLowerInvariant();
        var tail = positionals.Count <= 1 ? Array.Empty<string>() : positionals.Skip(1).ToArray();
        return new TerminalCommand(name, tail, values, flags);
    }
}
