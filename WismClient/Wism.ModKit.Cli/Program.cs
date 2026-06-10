using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Wism.Client.Modules.Profiles;
using Wism.Client.Modules.Worlds;
using Wism.ModKit.Cli;

var command = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)) ?? "validate";
var options = CliOptions.Parse(args);

try
{
    return command.ToLowerInvariant() switch
    {
        "validate" => RunValidate(options),
        "proof" => RunProof(options),
        "world" => RunWorld(args, options),
        _ => Usage()
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    if (options.Json)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            status = "Failed",
            error = ex.Message
        }, JsonOptions()));
    }

    return 1;
}

static int RunWorld(string[] args, CliOptions options)
{
    var subcommand = args
        .Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase))
        .Where(arg => !arg.Contains("=", StringComparison.OrdinalIgnoreCase))
        .Skip(1)
        .FirstOrDefault() ?? "validate";
    if (string.Equals(subcommand, "build-near-illuria", StringComparison.OrdinalIgnoreCase))
    {
        return RunWorldBuildNearIlluria(options);
    }

    if (!string.Equals(subcommand, "validate", StringComparison.OrdinalIgnoreCase))
    {
        return Usage();
    }

    var modRoot = string.IsNullOrWhiteSpace(options.ModRoot)
        ? ModularGameProfileCatalog.ResolveModRoot(options.RepositoryRoot)
        : Path.GetFullPath(options.ModRoot);
    var report = WorldKitValidator.ValidateModRoot(
        modRoot,
        options.WorldId,
        new WorldKitValidationOptions
        {
            RequestedPlayers = options.RequestedPlayers,
            ActiveClans = options.ActiveClans
        });

    if (options.Json)
    {
        Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions()));
    }
    else
    {
        PrintWorldValidation(report);
    }

    return report.IsValid ? 0 : 1;
}

static int RunWorldBuildNearIlluria(CliOptions options)
{
    var result = NearIlluriaKitBuilder.Build(new NearIlluriaKitBuildOptions
    {
        RepositoryRoot = options.RepositoryRoot,
        UnityModRoot = options.UnityModRoot,
        SourceWorld = options.SourceWorldId,
        World = options.WorldWasSpecified ? options.WorldId : string.Empty,
        Profile = options.ProfileWasSpecified ? options.ProfileId : string.Empty,
        CopyToUnity = options.CopyToUnity
    });

    if (options.Json)
    {
        Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions()));
    }
    else
    {
        Console.WriteLine($"Near-Illuria kit build: {result.Status}");
        Console.WriteLine($"  Source world: {result.SourceWorld}");
        Console.WriteLine($"  World: {result.World}");
        Console.WriteLine($"  Profile: {result.Profile}");
        Console.WriteLine($"  Map: {result.Width} x {result.Height}");
        Console.WriteLine($"  Cities: {result.CityCount} ({result.NavyCityCount} navy-capable, {result.AdjustedCityCount} adjusted)");
        Console.WriteLine($"  Locations: {result.LocationCount}");
        foreach (var path in result.WrittenFiles)
        {
            Console.WriteLine($"  Wrote: {path}");
        }
    }

    return 0;
}

static int RunValidate(CliOptions options)
{
    var result = Validate(options);
    if (options.Json)
    {
        Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions()));
    }
    else
    {
        PrintValidation(result);
    }

    return result.IsValid ? 0 : 1;
}

static int RunProof(CliOptions options)
{
    var runId = string.IsNullOrWhiteSpace(options.RunId)
        ? $"modkit-proof-{DateTime.UtcNow:yyyyMMdd-HHmmss}"
        : options.RunId;
    var outputRoot = string.IsNullOrWhiteSpace(options.Output)
        ? Path.Combine(options.RepositoryRoot, "artifacts", "mod-kit")
        : options.Output;
    var outputDirectory = Path.GetFullPath(Path.Combine(outputRoot, runId));
    Directory.CreateDirectory(outputDirectory);

    var commands = new List<string>
    {
        $"Wism.ModKit.Cli validate repo={options.RepositoryRoot} profile={options.ProfileId} packs={string.Join(",", options.PackIds)} --json"
    };
    if (!string.IsNullOrWhiteSpace(options.UnityTestResults))
    {
        commands.Add($"Unity PlayMode ModSettingsUiTests results={options.UnityTestResults}");
    }

    File.WriteAllLines(Path.Combine(outputDirectory, "commands.txt"), commands);

    var validation = Validate(options);
    var validationPath = Path.Combine(outputDirectory, "cli-report.json");
    File.WriteAllText(validationPath, JsonSerializer.Serialize(validation, JsonOptions()));

    ProofStepResult agentResult = null;
    if (options.RunAgentPlayground)
    {
        agentResult = RunAgentPlayground(options, outputDirectory);
        commands.Add(agentResult.Command);
        File.WriteAllLines(Path.Combine(outputDirectory, "commands.txt"), commands);
    }

    var unityStatus = ReadUnityProof(options.UnityStatusManifest);
    var unityRuntime = ReadUnityProof(options.UnityManifest);
    var unityTests = ReadUnityTestResults(options.UnityTestResults);
    var unityStatusRequired = !string.IsNullOrWhiteSpace(options.UnityStatusManifest);
    var unityTestsRequired = !string.IsNullOrWhiteSpace(options.UnityTestResults);
    var unityPassed = string.Equals(unityRuntime.Status, "Passed", StringComparison.OrdinalIgnoreCase) &&
                      (!unityStatusRequired || string.Equals(unityStatus.Status, "Passed", StringComparison.OrdinalIgnoreCase));
    var unityTestsPassed = unityTestsRequired &&
                           string.Equals(unityTests.Status, "Passed", StringComparison.OrdinalIgnoreCase);
    var status = validation.IsValid &&
                 (agentResult == null || agentResult.ExitCode == 0) &&
                 unityPassed &&
                 unityTestsPassed
        ? "Green"
        : !validation.IsValid ||
          (agentResult != null && agentResult.ExitCode != 0) ||
          (unityTestsRequired && !unityTestsPassed)
            ? "Red"
            : "Yellow";

    var summary = new
    {
        schemaVersion = 1,
        workItemId = "WISM-MODKIT-E2E-001",
        status,
        runId,
        startedAtUtc = DateTime.UtcNow.ToString("O"),
        repositoryRoot = options.RepositoryRoot,
        outputDirectory,
        validation = new
        {
            validation.Status,
            validation.IsValid,
            validation.IssueCount,
            path = validationPath
        },
        agentPlayground = agentResult,
        unity = unityRuntime,
        unityStatus,
        unityRuntime,
        unityTests,
        notes = status == "Yellow"
            ? "Unity runtime proof is missing or not passed, a supplied Unity status proof did not pass, or Unity UI test proof was not supplied; this proof bundle is not Green."
            : string.Empty
    };

    var summaryPath = Path.Combine(outputDirectory, "proof-summary.json");
    File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, JsonOptions()));

    if (options.Json)
    {
        Console.WriteLine(File.ReadAllText(summaryPath));
    }
    else
    {
        Console.WriteLine($"Mod Kit proof: {status}");
        Console.WriteLine($"  Summary: {summaryPath}");
        Console.WriteLine($"  Validation: {validation.Status}");
        Console.WriteLine($"  AgentPlayground: {(agentResult == null ? "Skipped" : agentResult.Status)}");
        Console.WriteLine($"  Unity status: {(unityStatusRequired ? unityStatus.Status : "Not supplied")}");
        Console.WriteLine($"  Unity runtime: {unityRuntime.Status}");
        Console.WriteLine($"  Unity tests: {(unityTestsRequired ? $"{unityTests.Status} ({unityTests.Passed}/{unityTests.Total})" : "Not supplied")}");
    }

    return status == "Red" ? 1 : 0;
}

static ValidationCliResult Validate(CliOptions options)
{
    var modRoot = string.IsNullOrWhiteSpace(options.ModRoot)
        ? ModularGameProfileCatalog.ResolveModRoot(options.RepositoryRoot)
        : Path.GetFullPath(options.ModRoot);
    var report = ModKitValidator.ValidateModRoot(modRoot);
    ModularGameProfileSelection selection = null;
    string selectionError = null;
    ModKitCompatibilityReport compatibility = null;

    try
    {
        selection = ModularGameProfileCatalog.ResolveFromModRoot(modRoot, options.ProfileId, options.PackIds);
        var world = !string.IsNullOrWhiteSpace(selection.Launch?.World)
            ? selection.Launch.World
            : selection.BaseWorld;
        compatibility = ModKitSelectionService.VerifySelection(modRoot, options.ProfileId, options.PackIds, world);
    }
    catch (Exception ex)
    {
        selectionError = ex.Message;
    }

    var issues = report.Issues.Select(issue => new ValidationIssueDto
    {
        Severity = issue.Severity.ToString(),
        Code = issue.Code,
        Message = issue.Message,
        Path = issue.Path
    }).ToArray();
    if (!string.IsNullOrWhiteSpace(selectionError))
    {
        issues = issues.Concat(new[]
        {
            new ValidationIssueDto
            {
                Severity = ModKitValidationSeverity.Error.ToString(),
                Code = "selection-invalid",
                Message = selectionError,
                Path = modRoot
            }
        }).ToArray();
    }

    var isValid = report.IsValid && string.IsNullOrWhiteSpace(selectionError);
    return new ValidationCliResult
    {
        SchemaVersion = 1,
        Status = isValid ? "Passed" : "Failed",
        IsValid = isValid,
        RepositoryRoot = options.RepositoryRoot,
        ModRoot = modRoot,
        ProfileId = options.ProfileId,
        PackIds = options.PackIds,
        BaseWorld = selection?.BaseWorld ?? string.Empty,
        LaunchWorld = selection?.Launch?.World ?? string.Empty,
        CompatibilityStatus = compatibility?.Status.ToString() ?? string.Empty,
        IsGreen = compatibility?.IsGreen ?? false,
        IsLoadable = compatibility?.IsLoadable ?? isValid,
        ContentFingerprint = compatibility?.Selection?.ContentFingerprint ?? string.Empty,
        IssueCount = issues.Length,
        Issues = issues
    };
}

static ProofStepResult RunAgentPlayground(CliOptions options, string outputDirectory)
{
    var logPath = Path.Combine(outputDirectory, "agentplayground-report.txt");
    var wismClientRoot = FindWismClientRoot(options.RepositoryRoot);
    var project = Path.Combine(wismClientRoot, "Wism.Agent.Playground");
    var arguments =
        $"run --project \"{project}\" -- world profile={options.ProfileId} packs={string.Join(",", options.PackIds)} --quiet";
    var command = $"dotnet {arguments}";
    var start = new ProcessStartInfo("dotnet", arguments)
    {
        WorkingDirectory = wismClientRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    using var process = Process.Start(start);
    var output = process!.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    File.WriteAllText(logPath, output + Environment.NewLine + error);
    return new ProofStepResult
    {
        Status = process.ExitCode == 0 ? "Passed" : "Failed",
        ExitCode = process.ExitCode,
        Command = command,
        LogPath = logPath
    };
}

static string FindWismClientRoot(string repositoryRoot)
{
    if (Directory.Exists(Path.Combine(repositoryRoot, "Wism.Agent.Playground")) &&
        Directory.Exists(Path.Combine(repositoryRoot, "Wism.Client.Core")))
    {
        return repositoryRoot;
    }

    return Path.Combine(repositoryRoot, "WismClient");
}

static UnityProofResult ReadUnityProof(string manifestPath)
{
    if (string.IsNullOrWhiteSpace(manifestPath))
    {
        return new UnityProofResult
        {
            Status = "Missing",
            ManifestPath = string.Empty,
            Notes = "unityManifest=<path> was not provided."
        };
    }

    if (!File.Exists(manifestPath))
    {
        return new UnityProofResult
        {
            Status = "Missing",
            ManifestPath = manifestPath,
            Notes = "Unity manifest path was provided but the file was not found."
        };
    }

    try
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        var status = ReadString(root, "status", "Unknown");
        return new UnityProofResult
        {
            Status = status,
            ManifestPath = manifestPath,
            UnityVersion = ReadString(root, "unityVersion", string.Empty),
            World = ReadString(root, "world", ReadNestedString(root, "selection", "worldName", string.Empty)),
            Profile = ReadString(root, "profile", ReadNestedString(root, "selection", "profileId", string.Empty)),
            CompatibilityStatus = ReadNestedString(root, "selection", "compatibilityStatus", string.Empty),
            IsGreen = ReadNestedBool(root, "selection", "isGreen", false),
            ContentFingerprint = ReadNestedString(root, "selection", "contentFingerprint", string.Empty),
            DirtySceneCount = ReadArrayLength(root, "dirtyScenes"),
            ErrorCount = ReadNestedInt(root, "console", "errors", 0),
            WarningCount = ReadNestedInt(root, "console", "warnings", 0),
            Notes = status == "Passed" ? string.Empty : "Unity manifest did not report Passed."
        };
    }
    catch (JsonException ex)
    {
        return new UnityProofResult
        {
            Status = "Unreadable",
            ManifestPath = manifestPath,
            Notes = ex.Message
        };
    }
}

static string ReadString(JsonElement root, string name, string fallback)
{
    return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
        ? value.GetString() ?? fallback
        : fallback;
}

static string ReadNestedString(JsonElement root, string parentName, string name, string fallback)
{
    return root.TryGetProperty(parentName, out var parent) && parent.ValueKind == JsonValueKind.Object
        ? ReadString(parent, name, fallback)
        : fallback;
}

static int ReadNestedInt(JsonElement root, string parentName, string name, int fallback)
{
    if (!root.TryGetProperty(parentName, out var parent) || parent.ValueKind != JsonValueKind.Object)
    {
        return fallback;
    }

    return parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed)
        ? parsed
        : fallback;
}

static UnityTestProofResult ReadUnityTestResults(string resultsPath)
{
    if (string.IsNullOrWhiteSpace(resultsPath))
    {
        return new UnityTestProofResult
        {
            Status = "Missing",
            ResultsPath = string.Empty,
            Notes = "unityTestResults=<path> was not provided."
        };
    }

    if (!File.Exists(resultsPath))
    {
        return new UnityTestProofResult
        {
            Status = "Missing",
            ResultsPath = resultsPath,
            Notes = "Unity test results path was provided but the file was not found."
        };
    }

    try
    {
        var document = XDocument.Load(resultsPath);
        var root = document.Root;
        if (root == null)
        {
            return new UnityTestProofResult
            {
                Status = "Unreadable",
                ResultsPath = resultsPath,
                Notes = "Unity test results XML had no root element."
            };
        }

        var result = ReadAttribute(root, "result", "Unknown");
        var failed = ReadIntAttribute(root, "failed", 0);
        var status = string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase) && failed == 0
            ? "Passed"
            : "Failed";

        return new UnityTestProofResult
        {
            Status = status,
            ResultsPath = resultsPath,
            Result = result,
            Total = ReadIntAttribute(root, "total", 0),
            Passed = ReadIntAttribute(root, "passed", 0),
            Failed = failed,
            Skipped = ReadIntAttribute(root, "skipped", 0),
            DurationSeconds = ReadDoubleAttribute(root, "duration", 0),
            Notes = status == "Passed" ? string.Empty : "Unity test results did not report Passed."
        };
    }
    catch (XmlException ex)
    {
        return new UnityTestProofResult
        {
            Status = "Unreadable",
            ResultsPath = resultsPath,
            Notes = ex.Message
        };
    }
}

static bool ReadNestedBool(JsonElement root, string parentName, string name, bool fallback)
{
    if (!root.TryGetProperty(parentName, out var parent) || parent.ValueKind != JsonValueKind.Object)
    {
        return fallback;
    }

    return parent.TryGetProperty(name, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
        ? value.GetBoolean()
        : fallback;
}

static int ReadArrayLength(JsonElement root, string name)
{
    return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
        ? value.GetArrayLength()
        : 0;
}

static string ReadAttribute(XElement element, string name, string fallback)
{
    return element.Attribute(name)?.Value ?? fallback;
}

static int ReadIntAttribute(XElement element, string name, int fallback)
{
    return int.TryParse(ReadAttribute(element, name, string.Empty), out var value)
        ? value
        : fallback;
}

static double ReadDoubleAttribute(XElement element, string name, double fallback)
{
    return double.TryParse(ReadAttribute(element, name, string.Empty), out var value)
        ? value
        : fallback;
}

static void PrintValidation(ValidationCliResult result)
{
    Console.WriteLine($"Mod Kit validation: {result.Status}");
    Console.WriteLine($"  Mod root: {result.ModRoot}");
    Console.WriteLine($"  Profile: {result.ProfileId}");
    Console.WriteLine($"  Packs: {(result.PackIds.Length == 0 ? "(none)" : string.Join(", ", result.PackIds))}");
    Console.WriteLine($"  Base world: {result.BaseWorld}");
    Console.WriteLine($"  Launch world: {result.LaunchWorld}");
    Console.WriteLine($"  Compatibility: {result.CompatibilityStatus}");
    Console.WriteLine($"  Green: {result.IsGreen}");
    Console.WriteLine($"  Fingerprint: {result.ContentFingerprint}");
    Console.WriteLine($"  Issues: {result.IssueCount}");
    foreach (var issue in result.Issues)
    {
        Console.WriteLine($"  {issue.Severity} {issue.Code}: {issue.Message} ({issue.Path})");
    }
}

static void PrintWorldValidation(WorldKitValidationReport report)
{
    Console.WriteLine($"World Kit validation: {report.Status}");
    Console.WriteLine($"  World: {report.WorldId}");
    Console.WriteLine($"  World root: {report.WorldRoot}");
    Console.WriteLine($"  Map: {report.Coverage.Width} x {report.Coverage.Height} ({report.Coverage.TileCount}/{report.Coverage.ExpectedTileCount} tiles)");
    Console.WriteLine($"  Cities: {report.Coverage.CityCount}");
    Console.WriteLine($"  Locations: {report.Coverage.LocationCount}");
    Console.WriteLine($"  Clan starts: {report.Coverage.ClansWithStarts}");
    Console.WriteLine($"  Reachable city pairs: {report.Coverage.ReachableCityPairs}/{report.Coverage.TotalCityPairs}");
    Console.WriteLine($"  Issues: {report.IssueCount}");
    foreach (var issue in report.Issues)
    {
        Console.WriteLine($"  {issue}");
    }
}

static int Usage()
{
    Console.WriteLine("Usage: Wism.ModKit.Cli [validate|proof] [repo=path] [modRoot=path] [profile=classic-warlords] [packs=a,b] [out=path] [runId=id] [unityManifest=path] [unityStatusManifest=path] [unityTestResults=path] [runAgent=true] [--json]");
    Console.WriteLine("       Wism.ModKit.Cli world validate [repo=path] [modRoot=path] [world=TestWorld] [players=N] [clans=a,b] [--json]");
    Console.WriteLine("       Wism.ModKit.Cli world build-near-illuria [repo=path] [source=Illuria] [world=Near-Illuria] [profile=near-illuria] [unityModRoot=path] [copyUnity=true] [--json]");
    return 2;
}

static JsonSerializerOptions JsonOptions() => new()
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
};

sealed class CliOptions
{
    public string RepositoryRoot { get; private set; } = FindRepositoryRoot();
    public string ModRoot { get; private set; } = string.Empty;
    public string ProfileId { get; private set; } = ModularGameProfileCatalog.DefaultProfileId;
    public string[] PackIds { get; private set; } = Array.Empty<string>();
    public string Output { get; private set; } = string.Empty;
    public string RunId { get; private set; } = string.Empty;
    public string UnityManifest { get; private set; } = string.Empty;
    public string UnityStatusManifest { get; private set; } = string.Empty;
    public string UnityTestResults { get; private set; } = string.Empty;
    public string WorldId { get; private set; } = "TestWorld";
    public string SourceWorldId { get; private set; } = "Illuria";
    public string UnityModRoot { get; private set; } = string.Empty;
    public int RequestedPlayers { get; private set; }
    public string[] ActiveClans { get; private set; } = Array.Empty<string>();
    public bool Json { get; private set; }
    public bool RunAgentPlayground { get; private set; } = true;
    public bool CopyToUnity { get; private set; } = true;
    public bool ProfileWasSpecified { get; private set; }
    public bool WorldWasSpecified { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var values = args
            .Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase))
            .Select(arg => arg.Split(new[] { '=' }, 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

        var options = new CliOptions
        {
            Json = args.Any(arg => string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase))
        };

        options.RepositoryRoot = Read(values, "repo", options.RepositoryRoot);
        options.ModRoot = Read(values, "modRoot", options.ModRoot);
        options.ProfileId = Read(values, "profile", options.ProfileId);
        options.ProfileWasSpecified = values.ContainsKey("profile");
        options.PackIds = ReadCsv(values, "packs");
        options.Output = Read(values, "out", options.Output);
        options.RunId = Read(values, "runId", options.RunId);
        options.UnityManifest = Read(values, "unityManifest", options.UnityManifest);
        options.UnityStatusManifest = Read(values, "unityStatusManifest", options.UnityStatusManifest);
        options.UnityTestResults = Read(values, "unityTestResults", options.UnityTestResults);
        options.WorldId = Read(values, "world", options.WorldId);
        options.WorldWasSpecified = values.ContainsKey("world");
        options.SourceWorldId = Read(values, "source", options.SourceWorldId);
        options.UnityModRoot = Read(values, "unityModRoot", options.UnityModRoot);
        options.RequestedPlayers = ReadInt(values, "players", options.RequestedPlayers);
        options.ActiveClans = ReadCsv(values, "clans");
        options.RunAgentPlayground = ReadBool(values, "runAgent", options.RunAgentPlayground);
        options.CopyToUnity = ReadBool(values, "copyUnity", options.CopyToUnity);
        return options;
    }

    static string Read(IReadOnlyDictionary<string, string> values, string name, string fallback)
    {
        return values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    static bool ReadBool(IReadOnlyDictionary<string, string> values, string name, bool fallback)
    {
        return values.TryGetValue(name, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    static int ReadInt(IReadOnlyDictionary<string, string> values, string name, int fallback)
    {
        return values.TryGetValue(name, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    static string[] ReadCsv(IReadOnlyDictionary<string, string> values, string name)
    {
        if (!values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current != null)
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
}

sealed class ValidationCliResult
{
    public int SchemaVersion { get; set; }
    public string Status { get; set; }
    public bool IsValid { get; set; }
    public string RepositoryRoot { get; set; }
    public string ModRoot { get; set; }
    public string ProfileId { get; set; }
    public string[] PackIds { get; set; }
    public string BaseWorld { get; set; }
    public string LaunchWorld { get; set; }
    public string CompatibilityStatus { get; set; }
    public bool IsGreen { get; set; }
    public bool IsLoadable { get; set; }
    public string ContentFingerprint { get; set; }
    public int IssueCount { get; set; }
    public ValidationIssueDto[] Issues { get; set; }
}

sealed class ValidationIssueDto
{
    public string Severity { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string Path { get; set; }
}

sealed class ProofStepResult
{
    public string Status { get; set; }
    public int ExitCode { get; set; }
    public string Command { get; set; }
    public string LogPath { get; set; }
}

sealed class UnityProofResult
{
    public string Status { get; set; }
    public string ManifestPath { get; set; }
    public string UnityVersion { get; set; }
    public string World { get; set; }
    public string Profile { get; set; }
    public string CompatibilityStatus { get; set; }
    public bool IsGreen { get; set; }
    public string ContentFingerprint { get; set; }
    public int DirtySceneCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public string Notes { get; set; }
}

sealed class UnityTestProofResult
{
    public string Status { get; set; }
    public string ResultsPath { get; set; }
    public string Result { get; set; }
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public double DurationSeconds { get; set; }
    public string Notes { get; set; }
}
