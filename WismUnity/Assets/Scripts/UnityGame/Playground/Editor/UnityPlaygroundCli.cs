using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.Persistance.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wism.Client.Core;

namespace WismUnity.Playground
{
    public static class UnityPlaygroundCli
    {
        const string DefaultWorld = "TestWorld";
        const string DefaultScene = "Assets/Scenes/Test/TestWorld.unity";

        public static void Run()
        {
            var exitCode = 0;
            var options = UnityPlaygroundOptions.FromCommandLine(Environment.GetCommandLineArgs());
            var report = new UnityPlaygroundReport
            {
                schemaVersion = 1,
                command = "world",
                world = options.World,
                scenePath = options.ScenePath,
                runId = options.RunId,
                startedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                batchMode = Application.isBatchMode,
                artifactDirectory = options.OutputDirectory
            };

            try
            {
                Directory.CreateDirectory(options.OutputDirectory);
                RunWorldSmoke(options, report);
                if (string.Equals(report.status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    exitCode = 1;
                }
            }
            catch (Exception ex)
            {
                exitCode = 1;
                report.status = "Failed";
                report.outcome = ex.Message;
                report.events.Add(ex.ToString());
                Debug.LogException(ex);
            }
            finally
            {
                report.finishedAtUtc = DateTime.UtcNow.ToString("O");
                WriteManifest(options.OutputDirectory, report);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(exitCode);
                }
            }
        }

        [MenuItem("WISM/Playground/Run TestWorld Smoke")]
        public static void RunMenuSmoke()
        {
            Run();
        }

        static void RunWorldSmoke(UnityPlaygroundOptions options, UnityPlaygroundReport report)
        {
            if (!File.Exists(options.ScenePath))
            {
                throw new FileNotFoundException($"Scene not found: {options.ScenePath}");
            }

            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var preExistingDirtyScenes = GetLoadedDirtyScenes();
            var scene = default(Scene);
            try
            {
                report.events.Add($"Loading scene additively: {options.ScenePath}");
                scene = EditorSceneManager.OpenScene(options.ScenePath, OpenSceneMode.Additive);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    throw new InvalidOperationException($"Scene did not load: {options.ScenePath}");
                }

                var unityManager = FindUnityManager(scene);
                report.scene = SceneSummary(scene);
                report.events.Add("Found UnityManager.");

                UnityManager.SetNewGameSettings(CreateSettings(options.World));
                unityManager.Initialize(CreateSettings(options.World));
                report.events.Add("Initialized UnityManager with deterministic playground settings.");

                if (options.AdvanceBootstrap)
                {
                    unityManager.FixedUpdate();
                    unityManager.FixedUpdate();
                    report.events.Add("Advanced UnityManager bootstrap with two FixedUpdate calls.");
                }

                report.events.Add($"UnityManager execution mode: {unityManager.ExecutionMode}");

                report.game = GameSummary(unityManager);
                report.console = ConsoleSummary();

                if (options.CaptureScreenshot)
                {
                    report.screenshotPath = CaptureScreenshot(options.OutputDirectory, options.RunId);
                    report.events.Add($"Requested screenshot capture: {report.screenshotPath}");
                }

                var dirtyScenes = GetLoadedDirtyScenes()
                    .Where(path => !preExistingDirtyScenes.Contains(path, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
                report.dirtyScenes = dirtyScenes;
                if (dirtyScenes.Length > 0)
                {
                    report.status = "Failed";
                    report.outcome = "Unity Playground run dirtied one or more scenes.";
                    report.events.Add("Dirty scenes: " + string.Join(", ", dirtyScenes));
                    return;
                }

                report.status = "Passed";
                report.outcome = $"{options.World} loaded and initialized without dirtying loaded scenes.";
            }
            finally
            {
                report.events.Add("Restoring original editor scene setup without saving.");
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        static UnityNewGameEntity CreateSettings(string world)
        {
            return new UnityNewGameEntity
            {
                InteractiveUI = false,
                IsNewGame = true,
                RandomSeed = 1990,
                RandomStartLocations = false,
                WorldName = world,
                Players = new[]
                {
                    new UnityPlayerEntity { ClanName = "Sirians", IsHuman = true },
                    new UnityPlayerEntity { ClanName = "LordBane", IsHuman = false }
                }
            };
        }

        static UnityManager FindUnityManager(Scene scene)
        {
            var manager = scene.GetRootGameObjects()
                .SelectMany(Flatten)
                .Select(go => go.GetComponent<UnityManager>())
                .FirstOrDefault(component => component != null);

            return manager != null
                ? manager
                : throw new InvalidOperationException("Could not find a UnityManager in the loaded scene.");
        }

        static UnityPlaygroundSceneSummary SceneSummary(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var objects = roots.SelectMany(Flatten).ToArray();
            return new UnityPlaygroundSceneSummary
            {
                name = scene.name,
                path = scene.path,
                rootGameObjectCount = roots.Length,
                sceneGameObjectCount = objects.Length,
                isDirty = scene.isDirty
            };
        }

        static UnityPlaygroundGameSummary GameSummary(UnityManager unityManager)
        {
            var gameInitialized = Game.IsInitialized();
            var world = TryGetCurrentWorld();
            var worldInitialized = world != null && world.Map != null;
            return new UnityPlaygroundGameSummary
            {
                gameInitialized = gameInitialized,
                worldInitialized = worldInitialized,
                worldName = worldInitialized ? world.Name : string.Empty,
                mapWidth = worldInitialized ? world.Map.GetLength(0) : 0,
                mapHeight = worldInitialized ? world.Map.GetLength(1) : 0,
                cityCount = worldInitialized ? world.GetCities().Count : 0,
                locationCount = worldInitialized ? world.GetLocations().Count : 0,
                playerCount = gameInitialized ? Game.Current.Players.Count : 0,
                currentClan = gameInitialized ? Game.Current.GetCurrentPlayer().Clan.ShortName : string.Empty,
                executionMode = unityManager.ExecutionMode.ToString(),
                lastCommandId = unityManager.LastCommandId,
                interactiveUI = unityManager.InteractiveUI
            };
        }

        static World TryGetCurrentWorld()
        {
            try
            {
                return World.Current;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        static UnityPlaygroundConsoleSummary ConsoleSummary()
        {
            var summary = new UnityPlaygroundConsoleSummary();
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                var getCountsMethod = logEntriesType?.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (getCountsMethod == null)
                {
                    summary.available = false;
                    summary.note = "Unity console count API was unavailable.";
                    return summary;
                }

                var parameters = new object[] { 0, 0, 0 };
                getCountsMethod.Invoke(null, parameters);
                summary.available = true;
                summary.errors = (int)parameters[0];
                summary.warnings = (int)parameters[1];
                summary.logs = (int)parameters[2];
            }
            catch (Exception ex)
            {
                summary.available = false;
                summary.note = ex.Message;
            }

            return summary;
        }

        static string CaptureScreenshot(string outputDirectory, string runId)
        {
            var path = Path.Combine(outputDirectory, $"{runId}.png");
            ScreenCapture.CaptureScreenshot(path);
            return path;
        }

        static void WriteManifest(string outputDirectory, UnityPlaygroundReport report)
        {
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, "manifest.json"),
                JsonUtility.ToJson(report, true));
        }

        static IEnumerable<GameObject> Flatten(GameObject root)
        {
            yield return root;
            foreach (Transform child in root.transform)
            {
                foreach (var nested in Flatten(child.gameObject))
                {
                    yield return nested;
                }
            }
        }

        static string[] GetLoadedDirtyScenes()
        {
            var dirty = new List<string>();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty)
                {
                    dirty.Add(string.IsNullOrWhiteSpace(scene.path) ? scene.name : scene.path);
                }
            }

            return dirty.ToArray();
        }

        sealed class UnityPlaygroundOptions
        {
            public string World = DefaultWorld;
            public string ScenePath = DefaultScene;
            public string RunId = $"unity-smoke-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            public string OutputDirectory = Path.Combine("artifacts", "unity-playground", "smoke");
            public bool CaptureScreenshot;
            public bool AdvanceBootstrap;

            public static UnityPlaygroundOptions FromCommandLine(string[] args)
            {
                var options = new UnityPlaygroundOptions();
                var values = ParseArgs(args);
                options.World = Read(values, "world", options.World);
                options.ScenePath = Read(values, "scene", options.ScenePath);
                options.RunId = Read(values, "runId", options.RunId);
                options.CaptureScreenshot = ReadBool(values, "screenshot", false);
                options.AdvanceBootstrap = ReadBool(values, "advanceBootstrap", false);
                var outputRoot = Read(values, "out", options.OutputDirectory);
                options.OutputDirectory = Path.GetFullPath(Path.Combine(outputRoot, options.RunId));
                return options;
            }

            static Dictionary<string, string> ParseArgs(IEnumerable<string> args)
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var arg in args)
                {
                    var normalized = arg.TrimStart('-');
                    var separator = normalized.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    values[normalized.Substring(0, separator)] = normalized.Substring(separator + 1);
                }

                return values;
            }

            static string Read(Dictionary<string, string> values, string name, string fallback)
            {
                return values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : fallback;
            }

            static bool ReadBool(Dictionary<string, string> values, string name, bool fallback)
            {
                return values.TryGetValue(name, out var value) && bool.TryParse(value, out var parsed)
                    ? parsed
                    : fallback;
            }
        }
    }
}
