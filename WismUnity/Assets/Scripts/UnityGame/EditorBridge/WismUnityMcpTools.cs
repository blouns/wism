using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Unity.AI.MCP.Editor.Helpers;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.UnityGame.ModKit;
using Wism.Client.AI.Services;
using Wism.Client.AI.Tactical;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Pathing;

namespace WismUnity.EditorBridge
{
    public static class WismUnityMcpTools
    {
        const string Group = "wismunity";

        [McpTool("WismUnity.GetProjectStatus", "Returns public-safe WismUnity project, editor, build target, and active scene status.", Groups = new[] { Group, "editor" }, EnabledByDefault = true)]
        public static object GetProjectStatus()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var projectRoot = Directory.GetCurrentDirectory();

            return Response.Success("WismUnity project status loaded.", new
            {
                projectName = new DirectoryInfo(projectRoot).Name,
                unityVersion = Application.unityVersion,
                editorVersion = InternalEditorUtility.GetFullUnityVersion(),
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                activeScene = SceneInfo(scene),
                isPlaying = EditorApplication.isPlaying,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                hasUnsavedSceneChanges = scene.isDirty,
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetPackageStatus", "Returns installed package versions relevant to WismUnity and Unity AI Assistant integration.", Groups = new[] { Group, "packages" }, EnabledByDefault = true)]
        public static object GetPackageStatus()
        {
            var packages = new[]
            {
                "com.unity.ai.assistant",
                "com.unity.nuget.newtonsoft-json",
                "com.unity.ugui",
                "com.unity.test-framework"
            };

            return Response.Success("WismUnity package status loaded.", new
            {
                packages = packages.Select(PackageStatus).ToArray(),
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetSceneSummary", "Returns a read-only summary of the active scene hierarchy and WISM manager components.", Groups = new[] { Group, "scene" }, EnabledByDefault = true)]
        public static object GetSceneSummary()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Response.Success("WismUnity scene is not loaded.", new
                {
                    activeScene = SceneInfo(scene),
                    rootGameObjectCount = 0,
                    sceneGameObjectCount = 0,
                    managerCount = 0,
                    managers = Array.Empty<object>()
                });
            }

            var roots = scene.GetRootGameObjects();
            var sceneObjects = roots.SelectMany(Flatten).ToArray();
            var managers = sceneObjects
                .SelectMany(go => go.GetComponents<MonoBehaviour>()
                    .Where(component => component != null && component.GetType().Name.Contains("Manager"))
                    .Select(component => new
                    {
                        gameObject = HierarchyPath(component.gameObject),
                        type = component.GetType().FullName,
                        enabled = component.enabled
                    }))
                .OrderBy(manager => manager.type)
                .ThenBy(manager => manager.gameObject)
                .ToArray();

            return Response.Success("WismUnity scene summary loaded.", new
            {
                activeScene = SceneInfo(scene),
                rootGameObjectCount = roots.Length,
                sceneGameObjectCount = sceneObjects.Length,
                managerCount = managers.Length,
                managers
            });
        }

        [McpTool("WismUnity.GetConsoleSummary", "Returns Unity console counts without clearing or modifying console messages.", Groups = new[] { Group, "debug" }, EnabledByDefault = true)]
        public static object GetConsoleSummary()
        {
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType == null)
                    return Response.Error("CONSOLE_SUMMARY_UNAVAILABLE", new { reason = "UnityEditor.LogEntries type was not found." });

                var getCountsMethod = logEntriesType.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (getCountsMethod != null)
                {
                    var parameters = new object[] { 0, 0, 0 };
                    getCountsMethod.Invoke(null, parameters);
                    return Response.Success("Unity console summary loaded.", new
                    {
                        available = true,
                        errors = (int)parameters[0],
                        warnings = (int)parameters[1],
                        logs = (int)parameters[2],
                        timestampUtc = DateTime.UtcNow.ToString("O")
                    });
                }

                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var total = getCountMethod != null ? (int)getCountMethod.Invoke(null, null) : -1;
                return Response.Success("Unity console total count loaded.", new
                {
                    available = true,
                    errors = -1,
                    warnings = -1,
                    logs = -1,
                    totalEntries = total,
                    note = "Unity console per-type counts were unavailable for this editor version.",
                    timestampUtc = DateTime.UtcNow.ToString("O")
                });
            }
            catch (Exception ex)
            {
                return Response.Error("CONSOLE_SUMMARY_FAILED", new { reason = ex.Message });
            }
        }

        [McpTool("WismUnity.GetGameViewMetadata", "Returns read-only game view and camera metadata useful for visual smoke tests.", Groups = new[] { Group, "visual" }, EnabledByDefault = true)]
        public static object GetGameViewMetadata()
        {
            var mainCamera = Camera.main;
            var allCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Select(camera => new
                {
                    name = HierarchyPath(camera.gameObject),
                    enabled = camera.enabled,
                    tag = camera.tag,
                    targetTexture = camera.targetTexture != null ? camera.targetTexture.name : null,
                    orthographic = camera.orthographic,
                    orthographicSize = camera.orthographic ? camera.orthographicSize : 0f,
                    fieldOfView = camera.orthographic ? 0f : camera.fieldOfView,
                    depth = camera.depth
                })
                .OrderBy(camera => camera.depth)
                .ThenBy(camera => camera.name)
                .ToArray();

            return Response.Success("WismUnity game view metadata loaded.", new
            {
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                gameViewAspect = Screen.height > 0 ? Math.Round((double)Screen.width / Screen.height, 4) : 0,
                mainCamera = mainCamera != null ? HierarchyPath(mainCamera.gameObject) : null,
                cameraCount = allCameras.Length,
                cameras = allCameras,
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetWorldBuilderSummary", "Returns a read-only summary of the active WISM world-builder scene, scene containers, tilemap bounds, editor toggles, and MOD JSON availability.", Groups = new[] { Group, "world-builder", "scene" }, EnabledByDefault = true)]
        public static object GetWorldBuilderSummary()
        {
            var data = BuildWorldBuilderData();
            return Response.Success("WismUnity world-builder summary loaded.", data.ToSummaryObject());
        }

        [McpTool("WismUnity.ValidateWorldContract", "Validates the active WISM world-builder scene against read-only scene and MOD JSON contracts without mutating scenes or assets.", Groups = new[] { Group, "world-builder", "validation" }, EnabledByDefault = true)]
        public static object ValidateWorldContract()
        {
            var data = BuildWorldBuilderData();
            var issues = BuildWorldBuilderIssues(data);
            var errorCount = issues.Count(issue => issue.Severity == "Error");
            var warningCount = issues.Count(issue => issue.Severity == "Warning");

            return Response.Success("WismUnity world-builder contract validation loaded.", new
            {
                status = errorCount > 0 ? "Failed" : warningCount > 0 ? "NeedsAttention" : "Passed",
                readOnly = true,
                activeScene = data.ActiveScene,
                worldName = data.WorldName,
                issueCounts = new
                {
                    errors = errorCount,
                    warnings = warningCount,
                    informational = issues.Count(issue => issue.Severity == "Info")
                },
                issues = issues.Select(issue => issue.ToObject()).ToArray(),
                summary = data.ToSummaryObject(),
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetWorldBuilderRiskReport", "Returns read-only world-builder risks around scene/MOD drift, duplicate short names, and active editor import/reset toggles.", Groups = new[] { Group, "world-builder", "risk" }, EnabledByDefault = true)]
        public static object GetWorldBuilderRiskReport()
        {
            var data = BuildWorldBuilderData();
            var issues = BuildWorldBuilderIssues(data);
            var activeToggles = data.EditorToggles
                .Where(toggle => toggle.Enabled)
                .Select(toggle => toggle.ToObject())
                .ToArray();

            return Response.Success("WismUnity world-builder risk report loaded.", new
            {
                readOnly = true,
                activeScene = data.ActiveScene,
                worldName = data.WorldName,
                sceneDirty = data.SceneDirty,
                activeEditorToggles = activeToggles,
                riskLevel = issues.Any(issue => issue.Severity == "Error")
                    ? "High"
                    : issues.Any(issue => issue.Severity == "Warning")
                        ? "Medium"
                        : "Low",
                risks = issues
                    .Where(issue => issue.Severity != "Info")
                    .Select(issue => issue.ToObject())
                    .ToArray(),
                safeguards = new[]
                {
                    "Did not call WorldTilemap.CreateWorldFromScene.",
                    "Did not call map export routines.",
                    "Did not call Tilemap.CompressBounds.",
                    "Did not save scenes, prefabs, or MOD JSON.",
                    "Inspected UnityGame runtime components by reflection where possible."
                },
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        public static void RunWorldBuilderValidationBatchmode()
        {
            var args = Environment.GetCommandLineArgs();
            var sceneArg = GetCommandLineValue(args, "-wismWorldBuilderScenes")
                ?? "Assets/Scenes/Mini-Illuria.unity;Assets/Scenes/Test/TestWorld.unity";
            var reportPath = GetCommandLineValue(args, "-wismWorldBuilderReport")
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Logs", "worldbuilder-validation-report.json");

            var snapshots = new List<ValidationSnapshot>();
            foreach (var scenePath in sceneArg.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(path => path.Trim()))
            {
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
                {
                    snapshots.Add(new ValidationSnapshot
                    {
                        ScenePath = scenePath,
                        Status = "Failed",
                        Errors = 1,
                        Issues = new[] { WorldBuilderIssue.Error("SceneFileMissing", $"Scene file '{scenePath}' was not found.") }
                    });
                    continue;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var data = BuildWorldBuilderData();
                var issues = BuildWorldBuilderIssues(data);
                var errors = issues.Count(issue => issue.Severity == "Error");
                var warnings = issues.Count(issue => issue.Severity == "Warning");
                snapshots.Add(new ValidationSnapshot
                {
                    ScenePath = scenePath,
                    WorldName = data.WorldName,
                    Status = errors > 0 ? "Failed" : warnings > 0 ? "NeedsAttention" : "Passed",
                    Errors = errors,
                    Warnings = warnings,
                    CitySceneCount = data.CityEntries.EntryComponentCount,
                    LocationSceneCount = data.LocationEntries.EntryComponentCount,
                    CityModCount = data.ModWorld.CityCount,
                    LocationModCount = data.ModWorld.LocationCount,
                    TileCount = data.Tilemap.TileCount,
                    Issues = issues.ToArray()
                });
            }

            var directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(reportPath, BuildValidationReportJson(snapshots));
            Debug.Log($"WismUnity world-builder validation report written to {reportPath}");
        }

        [McpTool("WismUnity.GetWorldState", "Returns a read-only WISM game and world snapshot from the current Unity runtime state.", Groups = new[] { Group, "game" }, EnabledByDefault = true)]
        public static object GetWorldState()
        {
            if (!TryGetRuntime(out var game, out var world, out var unavailable))
                return unavailable;

            var map = world.Map;
            var currentPlayer = SafeCurrentPlayer(game);
            var selectedArmies = game.ArmiesSelected()
                ? game.GetSelectedArmies()
                : new List<Army>();

            return Response.Success("WISM world state loaded.", new
            {
                initialized = true,
                world = new
                {
                    name = world.Name,
                    width = map.GetLength(0),
                    height = map.GetLength(1),
                    cityCount = world.GetCities().Count,
                    locationCount = world.GetLocations().Count,
                    looseItemCount = world.GetLooseItems().Count
                },
                game = new
                {
                    state = game.GameState.ToString(),
                    randomSeed = game.RandomSeed,
                    currentPlayer = currentPlayer != null ? PlayerSummary(currentPlayer, true) : null,
                    selectedStack = StackSummary(selectedArmies)
                },
                players = game.Players
                    .Select(player => PlayerSummary(player, player == currentPlayer))
                    .ToArray(),
                cities = world.GetCities()
                    .OrderBy(city => city.ShortName)
                    .Select(CitySummary)
                    .ToArray(),
                locations = world.GetLocations()
                    .OrderBy(location => location.ShortName)
                    .Select(LocationSummary)
                    .ToArray(),
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetLegalActions", "Returns read-only legal action hints for the current WISM selection without enqueueing commands.", Groups = new[] { Group, "game", "actions" }, EnabledByDefault = true)]
        public static object GetLegalActions()
        {
            if (!TryGetRuntime(out var game, out var world, out var unavailable))
                return unavailable;

            if (!game.ArmiesSelected())
            {
                return Response.Success("No WISM armies are selected.", new
                {
                    initialized = true,
                    gameState = game.GameState.ToString(),
                    currentPlayer = PlayerSummary(game.GetCurrentPlayer(), true),
                    selected = false,
                    actions = new[]
                    {
                        new { kind = "select-next-army", legal = true, reason = "No selected stack; existing turn flow can select the next movable army." },
                        new { kind = "end-turn", legal = true, reason = "No selected stack is active." }
                    },
                    timestampUtc = DateTime.UtcNow.ToString("O")
                });
            }

            var armies = game.GetSelectedArmies();
            var origin = armies[0].Tile;
            var location = origin.Location;
            var city = origin.City;
            var currentPlayer = game.GetCurrentPlayer();
            var immediateActions = new List<object>
            {
                new { kind = "deselect-armies", legal = true, reason = "A stack is selected." },
                new { kind = "defend-armies", legal = true, reason = "A stack is selected." },
                new { kind = "quit-armies", legal = true, reason = "A stack is selected." }
            };

            if (location != null)
            {
                immediateActions.Add(new
                {
                    kind = "search-location",
                    legal = !location.Searched,
                    reason = location.Searched ? "Location has already been searched." : "Selected stack is on a searchable location.",
                    location = LocationSummary(location)
                });
            }

            if (city != null)
            {
                immediateActions.Add(new
                {
                    kind = "build-city-defense",
                    legal = city.Clan == currentPlayer.Clan && city.Defense < City.MaxDefense,
                    reason = city.Clan == currentPlayer.Clan ? "Selected stack is in a friendly city." : "Selected stack is not in a friendly city.",
                    city = CitySummary(city)
                });
                immediateActions.Add(new
                {
                    kind = "capture-city",
                    legal = city.Clan != currentPlayer.Clan && city.MusterArmies().All(army => army.Clan == currentPlayer.Clan),
                    reason = city.Clan == currentPlayer.Clan ? "City is already friendly." : "Selected stack is in a non-friendly city.",
                    city = CitySummary(city)
                });
            }

            return Response.Success("WISM legal action hints loaded.", new
            {
                initialized = true,
                gameState = game.GameState.ToString(),
                currentPlayer = PlayerSummary(currentPlayer, true),
                selected = true,
                selectedStack = StackSummary(armies),
                currentTile = TileSummary(origin),
                immediateActions = immediateActions.ToArray(),
                adjacentActions = AdjacentActionSummaries(world, armies, origin),
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.EvaluateBoard", "Returns read-only board evaluation metrics for the current WISM game state.", Groups = new[] { Group, "game", "ai" }, EnabledByDefault = true)]
        public static object EvaluateBoard()
        {
            if (!TryGetRuntime(out var game, out var world, out var unavailable))
                return unavailable;

            var currentPlayer = game.GetCurrentPlayer();
            var players = game.Players
                .Select(player => new
                {
                    player = PlayerSummary(player, player == currentPlayer),
                    military = new
                    {
                        armyCount = player.GetArmies().Count,
                        heroCount = player.GetHeros().Count,
                        totalStrength = player.GetArmies().Where(army => !army.IsDead).Sum(army => army.Strength),
                        totalMovesRemaining = player.GetArmies().Where(army => !army.IsDead).Sum(army => army.MovesRemaining)
                    },
                    economy = new
                    {
                        gold = player.Gold,
                        income = player.GetIncome(),
                        upkeep = player.GetUpkeep(),
                        netIncome = player.GetIncome() - player.GetUpkeep(),
                        cityCount = player.GetCities().Count
                    },
                    map = new
                    {
                        adjacentEnemyStacks = CountAdjacentEnemyStacks(world, player),
                        capturableCityCount = world.GetCities().Count(city => city.Clan != player.Clan)
                    }
                })
                .OrderByDescending(row => row.economy.cityCount)
                .ThenByDescending(row => row.military.totalStrength)
                .ToArray();

            return Response.Success("WISM board evaluation loaded.", new
            {
                initialized = true,
                world = new
                {
                    name = world.Name,
                    width = world.Map.GetLength(0),
                    height = world.Map.GetLength(1),
                    cityCount = world.GetCities().Count,
                    neutralCityCount = world.GetCities().Count(city => city.Clan == null),
                    searchableRemaining = world.GetLocations().Count(location => !location.Searched)
                },
                currentPlayer = PlayerSummary(currentPlayer, true),
                players,
                notes = new[]
                {
                    "Evaluation is read-only and uses current WismClient state.",
                    "Scores are descriptive metrics, not a game-state mutation or command execution."
                },
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.RunAITurnPreview", "Returns read-only tactical AI bid previews for the current WISM turn without adding commands.", Groups = new[] { Group, "game", "ai" }, EnabledByDefault = true)]
        public static object RunAITurnPreview()
        {
            if (!TryGetRuntime(out var game, out var world, out var unavailable))
                return unavailable;

            var loggerFactory = new WismLoggerFactory();
            var logger = loggerFactory.CreateLogger();
            var armyController = new ArmyController(loggerFactory);
            var pathingStrategy = new AStarPathingStrategy();
            var modules = new ITacticalModule[]
            {
                new CaptureModule(armyController, logger),
                new ExterminationModule(new PathfindingService(pathingStrategy), pathingStrategy, armyController, logger)
            };

            var bids = modules
                .SelectMany(module => SafeBids(module, world))
                .OrderByDescending(bid => bid.Utility)
                .ThenBy(bid => bid.Module.GetType().Name)
                .Take(24)
                .Select(BidSummary)
                .ToArray();

            return Response.Success("WISM AI tactical preview loaded.", new
            {
                initialized = true,
                previewAvailable = true,
                readOnly = true,
                currentPlayer = PlayerSummary(game.GetCurrentPlayer(), true),
                bidCount = bids.Length,
                bids,
                note = "Preview calls tactical bid generation only; it does not enqueue or execute commands.",
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetModKitStatus", "Returns a read-only Mod Kit profile, pack, validation, scene, and MOD data status report.", Groups = new[] { Group, "modkit" }, EnabledByDefault = true)]
        public static object GetModKitStatus(WismUnityModKitStatusRequest request)
        {
            try
            {
                request = request ?? new WismUnityModKitStatusRequest();
                var packIds = string.IsNullOrWhiteSpace(request.packs)
                    ? Array.Empty<string>()
                    : request.packs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(item => item.Trim())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToArray();
                var selection = UnityModKitSelection.Inspect(request.profile, packIds, request.world, request.modRoot);
                var scene = EditorSceneManager.GetActiveScene();

                return Response.Success("WismUnity Mod Kit status loaded.", new
                {
                    selection,
                    activeScene = SceneInfo(scene),
                    dirtyScenes = LoadedDirtyScenes(),
                    timestampUtc = DateTime.UtcNow.ToString("O")
                });
            }
            catch (Exception ex)
            {
                return Response.Error("MODKIT_STATUS_FAILED", new { reason = ex.Message });
            }
        }

        [Serializable]
        public sealed class WismUnityModKitStatusRequest
        {
            public string profile;
            public string packs;
            public string world;
            public string modRoot;
        }

        static WorldBuilderData BuildWorldBuilderData()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var data = new WorldBuilderData
            {
                ActiveScene = SceneInfo(scene),
                SceneDirty = scene.isDirty,
                SceneReady = scene.IsValid() && scene.isLoaded,
                ProjectRoot = Directory.GetCurrentDirectory()
            };

            if (!data.SceneReady)
                return data;

            var roots = scene.GetRootGameObjects();
            var sceneObjects = roots.SelectMany(Flatten).ToArray();

            data.UnityManager = BuildSceneObjectSummary(FindSceneObject(sceneObjects, "UnityManager", "UnityManager"));
            data.WorldTilemap = BuildSceneObjectSummary(FindSceneObject(sceneObjects, "WorldTilemap", "WorldTilemap"));
            data.Cities = BuildSceneObjectSummary(FindSceneObject(sceneObjects, "Cities", null));
            data.Locations = BuildSceneObjectSummary(FindSceneObject(sceneObjects, "Locations", null));
            data.WorldName = InferWorldName(data.UnityManager.GameObject, scene.name);
            data.ModPath = InferModPath(data.UnityManager.GameObject);
            data.Tilemap = BuildTilemapData(data.WorldTilemap.GameObject);
            data.CityEntries = BuildContainerEntries(data.Cities.GameObject, "CityEntry", "cityShortName");
            data.LocationEntries = BuildContainerEntries(data.Locations.GameObject, "LocationEntry", "locationShortName");
            data.EditorToggles = BuildEditorToggles(data.Cities.GameObject, data.Locations.GameObject);
            data.ModWorld = BuildModWorldData(data.ProjectRoot, data.WorldName, data.ModPath);

            return data;
        }

        static List<WorldBuilderIssue> BuildWorldBuilderIssues(WorldBuilderData data)
        {
            var issues = new List<WorldBuilderIssue>();

            if (!data.SceneReady)
            {
                issues.Add(WorldBuilderIssue.Error("SceneNotReady", "The active scene is invalid or not loaded."));
                return issues;
            }

            AddMissingObjectIssue(issues, data.UnityManager, "UnityManager");
            AddMissingObjectIssue(issues, data.WorldTilemap, "WorldTilemap");
            AddMissingObjectIssue(issues, data.Cities, "Cities");
            AddMissingObjectIssue(issues, data.Locations, "Locations");

            if (string.IsNullOrWhiteSpace(data.WorldName))
                issues.Add(WorldBuilderIssue.Error("MissingWorldName", "Could not infer a world name from GameManager.WorldName or the active scene name."));

            if (data.WorldTilemap.Found && !data.Tilemap.HasTilemapComponent)
                issues.Add(WorldBuilderIssue.Error("MissingTilemapComponent", "WorldTilemap exists but does not have a Tilemap component."));

            AddContainerIssues(issues, "City", data.CityEntries, data.ModWorld.CityShortNames, data.ModWorld.CityCount);
            AddContainerIssues(issues, "Location", data.LocationEntries, data.ModWorld.LocationShortNames, data.ModWorld.LocationCount);

            if (!data.ModWorld.AnyWorldDirectoryFound)
            {
                issues.Add(WorldBuilderIssue.Error("MissingModWorld", $"No MOD world directory was found for '{data.WorldName}'."));
            }
            else
            {
                if (!data.ModWorld.AnyCityJsonFound)
                    issues.Add(WorldBuilderIssue.Error("MissingCityJson", $"No City.json was found for '{data.WorldName}'."));

                if (!data.ModWorld.AnyLocationJsonFound)
                    issues.Add(WorldBuilderIssue.Error("MissingLocationJson", $"No Location.json was found for '{data.WorldName}'."));

                if (!data.ModWorld.AnyMapJsonFound)
                    issues.Add(WorldBuilderIssue.Warning("MissingMapJson", $"No Map.json was found for '{data.WorldName}' in the inspected MOD paths."));
            }

            foreach (var toggle in data.EditorToggles.Where(toggle => toggle.Enabled))
            {
                issues.Add(WorldBuilderIssue.Warning(
                    "EditorToggleActive",
                    $"{toggle.Container}.{toggle.Name} is enabled; editor import/reset controls can mutate scene objects during manual interaction."));
            }

            if (data.SceneDirty)
                issues.Add(WorldBuilderIssue.Warning("SceneDirty", "The active scene has unsaved changes."));

            if (issues.Count == 0)
                issues.Add(WorldBuilderIssue.Info("WorldContractClean", "The active world-builder scene matched the read-only checks."));

            return issues;
        }

        static void AddMissingObjectIssue(List<WorldBuilderIssue> issues, SceneObjectSummary summary, string name)
        {
            if (!summary.Found)
                issues.Add(WorldBuilderIssue.Error("MissingSceneObject", $"{name} was not found in the active scene."));
        }

        static void AddContainerIssues(
            List<WorldBuilderIssue> issues,
            string kind,
            ContainerEntries entries,
            string[] modShortNames,
            int modCount)
        {
            foreach (var duplicate in entries.DuplicateShortNames)
                issues.Add(WorldBuilderIssue.Error("DuplicateShortName", $"{kind} short name '{duplicate.ShortName}' appears {duplicate.Count} times in the scene."));

            if (entries.MissingShortNameCount > 0)
                issues.Add(WorldBuilderIssue.Warning("MissingShortName", $"{entries.MissingShortNameCount} {kind.ToLowerInvariant()} scene entries are missing short names."));

            if (entries.Found && entries.EntryComponentCount == 0)
                issues.Add(WorldBuilderIssue.Warning("EmptyContainer", $"{kind} container exists but no {kind}Entry components were found."));

            if (modCount >= 0 && entries.EntryComponentCount > 0 && modCount != entries.EntryComponentCount)
                issues.Add(WorldBuilderIssue.Warning("SceneModCountMismatch", $"{kind} scene entries ({entries.EntryComponentCount}) do not match MOD JSON rows ({modCount})."));

            var sceneNames = entries.ShortNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingFromMod = sceneNames
                .Where(name => !modShortNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                .OrderBy(name => name)
                .ToArray();

            if (missingFromMod.Length > 0)
                issues.Add(WorldBuilderIssue.Warning("SceneShortNamesMissingFromMod", $"{kind} scene short names missing from MOD JSON: {string.Join(", ", missingFromMod.Take(10))}."));
        }

        static GameObject FindSceneObject(IEnumerable<GameObject> sceneObjects, string name, string typeName)
        {
            var byName = sceneObjects.FirstOrDefault(go => string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase));
            if (byName != null)
                return byName;

            return string.IsNullOrWhiteSpace(typeName)
                ? null
                : sceneObjects.FirstOrDefault(go => FindComponentByTypeName(go, typeName) != null);
        }

        static SceneObjectSummary BuildSceneObjectSummary(GameObject gameObject)
        {
            if (gameObject == null)
                return SceneObjectSummary.Missing();

            return new SceneObjectSummary
            {
                Found = true,
                GameObject = gameObject,
                Name = gameObject.name,
                Path = HierarchyPath(gameObject),
                Tag = gameObject.tag,
                ActiveInHierarchy = gameObject.activeInHierarchy,
                ComponentTypes = gameObject.GetComponents<Component>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().FullName)
                    .OrderBy(type => type)
                    .ToArray()
            };
        }

        static TilemapData BuildTilemapData(GameObject worldTilemap)
        {
            var tilemap = worldTilemap != null ? worldTilemap.GetComponent<UnityEngine.Tilemaps.Tilemap>() : null;
            if (tilemap == null)
                return new TilemapData();

            var bounds = tilemap.cellBounds;
            var tileCount = 0;
            foreach (var position in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(position))
                    tileCount++;
            }

            return new TilemapData
            {
                HasTilemapComponent = true,
                Origin = new[] { bounds.x, bounds.y, bounds.z },
                Size = new[] { bounds.size.x, bounds.size.y, bounds.size.z },
                Min = new[] { bounds.xMin, bounds.yMin, bounds.zMin },
                Max = new[] { bounds.xMax, bounds.yMax, bounds.zMax },
                TileCount = tileCount
            };
        }

        static ContainerEntries BuildContainerEntries(GameObject container, string entryTypeName, string shortNameField)
        {
            var entries = new ContainerEntries { Found = container != null };
            if (container == null)
                return entries;

            entries.DirectChildCount = container.transform.Cast<Transform>().Count();
            var entryComponents = container.GetComponentsInChildren<Component>(true)
                .Where(component => component != null && component.GetType().Name == entryTypeName)
                .ToArray();

            entries.EntryComponentCount = entryComponents.Length;
            entries.ObjectsMissingEntryComponent = Math.Max(0, entries.DirectChildCount - entryComponents.Length);
            entries.SerializedTotal = GetIntMember(FindContainerComponent(container, entryTypeName), "total");

            foreach (var component in entryComponents)
            {
                var shortName = GetStringMember(component, shortNameField);
                if (string.IsNullOrWhiteSpace(shortName))
                    entries.MissingShortNameCount++;
                else
                    entries.ShortNames.Add(shortName);

                var position = component.transform.position;
                entries.Entries.Add(new SceneEntry
                {
                    Path = HierarchyPath(component.gameObject),
                    ShortName = shortName,
                    Position = new[] { (float)Math.Round(position.x, 3), (float)Math.Round(position.y, 3), (float)Math.Round(position.z, 3) }
                });
            }

            entries.DuplicateShortNames = entries.ShortNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => new DuplicateShortName { ShortName = group.Key, Count = group.Count() })
                .OrderBy(duplicate => duplicate.ShortName)
                .ToArray();

            return entries;
        }

        static EditorToggle[] BuildEditorToggles(GameObject cities, GameObject locations)
        {
            return new[]
            {
                BuildEditorToggle("Cities", cities, "importCitesFromTilemap"),
                BuildEditorToggle("Cities", cities, "resetCityObjects"),
                BuildEditorToggle("Locations", locations, "importLocationsFromTilemap"),
                BuildEditorToggle("Locations", locations, "resetLocationObjects")
            };
        }

        static EditorToggle BuildEditorToggle(string containerName, GameObject container, string toggleName)
        {
            var component = container != null
                ? container.GetComponents<Component>().FirstOrDefault(candidate => candidate != null && candidate.GetType().Name.EndsWith("Container", StringComparison.Ordinal))
                : null;

            return new EditorToggle
            {
                Container = containerName,
                Name = toggleName,
                Enabled = GetBoolMember(component, toggleName)
            };
        }

        static ModWorldData BuildModWorldData(string projectRoot, string worldName, string modPath)
        {
            var data = new ModWorldData();
            if (string.IsNullOrWhiteSpace(worldName))
                return data;

            var candidates = BuildModWorldCandidates(projectRoot, worldName, modPath);
            foreach (var candidatePath in candidates)
            {
                var candidate = new ModWorldCandidate
                {
                    Path = ProjectRelativePath(projectRoot, candidatePath),
                    Exists = Directory.Exists(candidatePath),
                    CityJson = BuildJsonFileSummary(projectRoot, Path.Combine(candidatePath, "City.json")),
                    LocationJson = BuildJsonFileSummary(projectRoot, Path.Combine(candidatePath, "Location.json")),
                    MapJson = BuildJsonFileSummary(projectRoot, Path.Combine(candidatePath, "Map.json"))
                };

                data.Candidates.Add(candidate);
            }

            data.AnyWorldDirectoryFound = data.Candidates.Any(candidate => candidate.Exists);
            data.AnyCityJsonFound = data.Candidates.Any(candidate => candidate.CityJson.Exists);
            data.AnyLocationJsonFound = data.Candidates.Any(candidate => candidate.LocationJson.Exists);
            data.AnyMapJsonFound = data.Candidates.Any(candidate => candidate.MapJson.Exists);

            var citySource = data.Candidates.FirstOrDefault(candidate => candidate.CityJson.Exists);
            if (citySource != null)
            {
                data.CityCount = citySource.CityJson.ObjectCount;
                data.CityShortNames = citySource.CityJson.ShortNames;
            }

            var locationSource = data.Candidates.FirstOrDefault(candidate => candidate.LocationJson.Exists);
            if (locationSource != null)
            {
                data.LocationCount = locationSource.LocationJson.ObjectCount;
                data.LocationShortNames = locationSource.LocationJson.ShortNames;
            }

            var mapSource = data.Candidates.FirstOrDefault(candidate => candidate.MapJson.Exists);
            if (mapSource != null)
                data.MapTileCount = CountArrayObjectsInProperty(NormalizeProjectPath(projectRoot, mapSource.MapJson.Path), "Tiles");

            return data;
        }

        static IEnumerable<string> BuildModWorldCandidates(string projectRoot, string worldName, string modPath)
        {
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(modPath))
            {
                var resolvedModPath = NormalizeProjectPath(projectRoot, modPath);
                candidates.Add(Path.Combine(resolvedModPath, ModFactory.WorldsPath, worldName));
                candidates.Add(Path.Combine(resolvedModPath, "Worlds", worldName));
            }

            candidates.Add(Path.Combine(projectRoot, "Assets", "Mod", "Worlds", worldName));
            candidates.Add(Path.Combine(projectRoot, "Assets", "Plugins", "WismClient", "Mods", "Worlds", worldName));

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        static JsonFileSummary BuildJsonFileSummary(string projectRoot, string path)
        {
            var summary = new JsonFileSummary
            {
                Path = ProjectRelativePath(projectRoot, path),
                Exists = File.Exists(path)
            };

            if (!summary.Exists)
                return summary;

            var text = File.ReadAllText(path);
            summary.Bytes = new FileInfo(path).Length;
            summary.ObjectCount = CountTopLevelArrayObjects(text);
            summary.ShortNames = Regex.Matches(text, "\"ShortName\"\\s*:\\s*\"(?<name>[^\"]+)\"")
                .Cast<Match>()
                .Select(match => match.Groups["name"].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToArray();

            return summary;
        }

        static int CountArrayObjectsInProperty(string path, string propertyName)
        {
            if (!File.Exists(path))
                return -1;

            var text = File.ReadAllText(path);
            var propertyIndex = text.IndexOf($"\"{propertyName}\"", StringComparison.Ordinal);
            if (propertyIndex < 0)
                return -1;

            var arrayStart = text.IndexOf('[', propertyIndex);
            if (arrayStart < 0)
                return -1;

            var depth = 0;
            var count = 0;
            var inString = false;
            var escaped = false;
            for (var i = arrayStart; i < text.Length; i++)
            {
                var ch = text[i];
                if (inString)
                {
                    escaped = !escaped && ch == '\\';
                    if (!escaped && ch == '"')
                        inString = false;
                    else if (ch != '\\')
                        escaped = false;
                    continue;
                }

                if (ch == '"')
                {
                    inString = true;
                    continue;
                }

                if (ch == '[' || ch == '{')
                {
                    depth++;
                    if (ch == '{' && depth == 2)
                        count++;
                }
                else if (ch == ']' || ch == '}')
                {
                    depth--;
                    if (depth == 0)
                        return count;
                }
            }

            return count;
        }

        static int CountTopLevelArrayObjects(string text)
        {
            var arrayStart = text.IndexOf('[');
            if (arrayStart < 0)
                return text.TrimStart().StartsWith("{", StringComparison.Ordinal) ? 1 : -1;

            var depth = 0;
            var count = 0;
            var inString = false;
            var escaped = false;
            for (var i = arrayStart; i < text.Length; i++)
            {
                var ch = text[i];
                if (inString)
                {
                    escaped = !escaped && ch == '\\';
                    if (!escaped && ch == '"')
                        inString = false;
                    else if (ch != '\\')
                        escaped = false;
                    continue;
                }

                if (ch == '"')
                {
                    inString = true;
                    continue;
                }

                if (ch == '[' || ch == '{')
                {
                    depth++;
                    if (ch == '{' && depth == 2)
                        count++;
                }
                else if (ch == ']' || ch == '}')
                {
                    depth--;
                    if (depth == 0)
                        return count;
                }
            }

            return count;
        }

        static Component FindContainerComponent(GameObject container, string entryTypeName)
        {
            if (container == null)
                return null;

            var expected = entryTypeName == "CityEntry" ? "CityContainer" : "LocationContainer";
            return FindComponentByTypeName(container, expected);
        }

        static Component FindComponentByTypeName(GameObject gameObject, string typeName)
        {
            if (gameObject == null)
                return null;

            return gameObject.GetComponents<Component>()
                .FirstOrDefault(component => component != null && component.GetType().Name == typeName);
        }

        static string InferWorldName(GameObject unityManager, string sceneName)
        {
            var gameManager = FindComponentByTypeName(unityManager, "GameManager");
            var worldName = GetStringMember(gameManager, "worldName");
            if (!string.IsNullOrWhiteSpace(worldName))
                return worldName;

            return string.Equals(sceneName, "Mini-Illuria", StringComparison.OrdinalIgnoreCase)
                ? "TestWorld"
                : sceneName;
        }

        static string InferModPath(GameObject unityManager)
        {
            var gameManager = FindComponentByTypeName(unityManager, "GameManager");
            return GetStringMember(gameManager, "modPath");
        }

        static string GetStringMember(object target, string name)
        {
            return GetMemberValue(target, name) as string;
        }

        static int GetIntMember(object target, string name)
        {
            var value = GetMemberValue(target, name);
            return value is int intValue ? intValue : -1;
        }

        static bool GetBoolMember(object target, string name)
        {
            var value = GetMemberValue(target, name);
            return value is bool boolValue && boolValue;
        }

        static object GetMemberValue(object target, string name)
        {
            if (target == null)
                return null;

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var property = type.GetProperty(name, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(target);

            var field = type.GetField(name, flags);
            return field?.GetValue(target);
        }

        static string GetCommandLineValue(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        static string BuildValidationReportJson(List<ValidationSnapshot> snapshots)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"readOnly\": true,");
            builder.AppendLine($"  \"timestampUtc\": \"{EscapeJson(DateTime.UtcNow.ToString("O"))}\",");
            builder.AppendLine("  \"scenes\": [");

            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                builder.AppendLine("    {");
                builder.AppendLine($"      \"scenePath\": \"{EscapeJson(snapshot.ScenePath)}\",");
                builder.AppendLine($"      \"worldName\": \"{EscapeJson(snapshot.WorldName)}\",");
                builder.AppendLine($"      \"status\": \"{EscapeJson(snapshot.Status)}\",");
                builder.AppendLine($"      \"errors\": {snapshot.Errors},");
                builder.AppendLine($"      \"warnings\": {snapshot.Warnings},");
                builder.AppendLine($"      \"citySceneCount\": {snapshot.CitySceneCount},");
                builder.AppendLine($"      \"locationSceneCount\": {snapshot.LocationSceneCount},");
                builder.AppendLine($"      \"cityModCount\": {snapshot.CityModCount},");
                builder.AppendLine($"      \"locationModCount\": {snapshot.LocationModCount},");
                builder.AppendLine($"      \"tileCount\": {snapshot.TileCount},");
                builder.AppendLine("      \"issues\": [");

                for (var issueIndex = 0; issueIndex < snapshot.Issues.Length; issueIndex++)
                {
                    var issue = snapshot.Issues[issueIndex];
                    builder.AppendLine("        {");
                    builder.AppendLine($"          \"severity\": \"{EscapeJson(issue.Severity)}\",");
                    builder.AppendLine($"          \"code\": \"{EscapeJson(issue.Code)}\",");
                    builder.AppendLine($"          \"message\": \"{EscapeJson(issue.Message)}\"");
                    builder.Append("        }");
                    builder.AppendLine(issueIndex == snapshot.Issues.Length - 1 ? string.Empty : ",");
                }

                builder.AppendLine("      ]");
                builder.Append("    }");
                builder.AppendLine(i == snapshots.Count - 1 ? string.Empty : ",");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        static string EscapeJson(string value)
        {
            if (value == null)
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        static string NormalizeProjectPath(string projectRoot, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(projectRoot, path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
        }

        static string ProjectRelativePath(string projectRoot, string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
            var normalizedPath = path.Replace('\\', '/');
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                ? normalizedPath.Substring(normalizedRoot.Length).TrimStart('/')
                : normalizedPath;
        }

        static object PackageStatus(string packageName)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            if (package == null)
                return new { name = packageName, installed = false };

            return new
            {
                name = package.name,
                installed = true,
                version = package.version,
                source = package.source.ToString(),
                resolvedPath = ProjectRelativePackagePath(package.resolvedPath)
            };
        }

        static object SceneInfo(Scene scene)
        {
            return new
            {
                name = scene.name,
                path = scene.path,
                isValid = scene.IsValid(),
                isLoaded = scene.isLoaded,
                isDirty = scene.isDirty,
                buildIndex = scene.buildIndex
            };
        }

        static string[] LoadedDirtyScenes()
        {
            var scenes = new List<string>();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty)
                {
                    scenes.Add(string.IsNullOrWhiteSpace(scene.path) ? scene.name : scene.path);
                }
            }

            return scenes.ToArray();
        }

        static IEnumerable<GameObject> Flatten(GameObject root)
        {
            yield return root;
            foreach (Transform child in root.transform)
            {
                foreach (var nested in Flatten(child.gameObject))
                    yield return nested;
            }
        }

        static string HierarchyPath(GameObject gameObject)
        {
            var names = new Stack<string>();
            var current = gameObject.transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        static string ProjectRelativePackagePath(string resolvedPath)
        {
            if (string.IsNullOrEmpty(resolvedPath))
                return resolvedPath;

            var projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            var normalized = resolvedPath.Replace('\\', '/');
            return normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(projectRoot.Length).TrimStart('/')
                : Path.GetFileName(normalized);
        }

        static bool TryGetRuntime(out Game game, out World world, out object unavailable)
        {
            game = null;
            world = null;

            if (!Game.IsInitialized())
            {
                unavailable = Response.Success("WISM game is not initialized.", new
                {
                    initialized = false,
                    reason = "Game.Current is not available. Enter Play Mode or initialize the Unity playground first.",
                    activeScene = SceneInfo(EditorSceneManager.GetActiveScene()),
                    timestampUtc = DateTime.UtcNow.ToString("O")
                });
                return false;
            }

            try
            {
                game = Game.Current;
                world = World.Current;
                unavailable = null;
                return true;
            }
            catch (Exception ex)
            {
                unavailable = Response.Error("WISM_RUNTIME_UNAVAILABLE", new { reason = ex.Message });
                return false;
            }
        }

        static Player SafeCurrentPlayer(Game game)
        {
            try
            {
                return game.GetCurrentPlayer();
            }
            catch
            {
                return null;
            }
        }

        static object PlayerSummary(Player player, bool isCurrent)
        {
            if (player == null)
                return null;

            return new
            {
                clan = ClanSummary(player.Clan),
                isCurrent,
                isHuman = player.IsHuman,
                isDead = player.IsDead,
                turn = player.Turn,
                gold = player.Gold,
                income = player.GetIncome(),
                upkeep = player.GetUpkeep(),
                cityCount = player.GetCities().Count,
                armyCount = player.GetArmies().Count,
                heroCount = player.GetHeros().Count,
                capitol = player.Capitol != null ? CitySummary(player.Capitol) : null
            };
        }

        static object ClanSummary(Clan clan)
        {
            if (clan == null)
                return null;

            return new
            {
                shortName = clan.ShortName,
                displayName = clan.DisplayName
            };
        }

        static object CitySummary(City city)
        {
            if (city == null)
                return null;

            return new
            {
                shortName = city.ShortName,
                displayName = city.DisplayName,
                owner = ClanSummary(city.Clan),
                x = city.X,
                y = city.Y,
                defense = city.Defense,
                income = city.Income,
                garrisonCount = city.MusterArmies().Count,
                production = ProductionSummary(city.Barracks)
            };
        }

        static object ProductionSummary(Barracks barracks)
        {
            if (barracks == null)
                return null;

            var productionKinds = barracks.GetProductionKinds()
                .Select(info => new
                {
                    armyInfoName = info.ArmyInfoName,
                    turnsToProduce = info.TurnsToProduce,
                    upkeep = info.Upkeep,
                    strength = info.Strength,
                    moves = info.Moves,
                    producedCount = barracks.GetProductionNumber(info.ArmyInfoName)
                })
                .Cast<object>()
                .ToArray();

            var armyInTraining = barracks.ArmyInTraining;

            return new
            {
                isProducing = barracks.ProducingArmy(),
                armyInTraining = armyInTraining != null
                    ? new
                    {
                        army = armyInTraining.ArmyInfo?.DisplayName,
                        armyShortName = armyInTraining.ArmyInfo?.ShortName,
                        turnsToProduce = armyInTraining.TurnsToProduce,
                        destinationCity = armyInTraining.DestinationCity?.ShortName
                    }
                    : null,
                deliveries = barracks.HasDeliveries()
                    ? barracks.ArmiesToDeliver
                        .Select(delivery => new
                        {
                            army = delivery.ArmyInfo?.DisplayName,
                            armyShortName = delivery.ArmyInfo?.ShortName,
                            turnsToDeliver = delivery.TurnsToDeliver,
                            destinationCity = delivery.DestinationCity?.ShortName
                        })
                        .Cast<object>()
                        .ToArray()
                    : Array.Empty<object>(),
                productionKinds
            };
        }

        static object LocationSummary(Location location)
        {
            if (location == null)
                return null;

            return new
            {
                shortName = location.ShortName,
                displayName = location.DisplayName,
                kind = location.Kind,
                x = location.X,
                y = location.Y,
                searched = location.Searched,
                hasMonster = location.HasMonster(),
                hasBoon = location.HasBoon()
            };
        }

        static object StackSummary(IReadOnlyList<Army> armies)
        {
            if (armies == null || armies.Count == 0)
            {
                return new
                {
                    count = 0,
                    x = 0,
                    y = 0,
                    owner = (object)null,
                    armies = Array.Empty<object>()
                };
            }

            var tile = armies[0].Tile;
            return new
            {
                count = armies.Count,
                x = tile?.X ?? 0,
                y = tile?.Y ?? 0,
                owner = ClanSummary(armies[0].Clan),
                totalStrength = armies.Where(army => !army.IsDead).Sum(army => army.Strength),
                minMovesRemaining = armies.Where(army => !army.IsDead).Select(army => army.MovesRemaining).DefaultIfEmpty(0).Min(),
                armies = armies.Select(ArmySummary).ToArray()
            };
        }

        static object ArmySummary(Army army)
        {
            if (army == null)
                return null;

            return new
            {
                id = army.Id,
                shortName = army.ShortName,
                displayName = army.DisplayName,
                kind = army.KindName,
                owner = ClanSummary(army.Clan),
                x = army.X,
                y = army.Y,
                strength = army.Strength,
                moves = army.Moves,
                movesRemaining = army.MovesRemaining,
                canWalk = army.CanWalk,
                canFloat = army.CanFloat,
                canFly = army.CanFly,
                isHero = army is Hero,
                isSpecial = army.IsSpecial(),
                isDefending = army.IsDefending,
                isDead = army.IsDead
            };
        }

        static object TileSummary(Tile tile)
        {
            if (tile == null)
                return null;

            return new
            {
                x = tile.X,
                y = tile.Y,
                terrain = tile.Terrain != null
                    ? new
                    {
                        shortName = tile.Terrain.ShortName,
                        displayName = tile.Terrain.DisplayName,
                        movementCost = tile.Terrain.MovementCost
                    }
                    : null,
                city = tile.City != null ? CitySummary(tile.City) : null,
                location = tile.Location != null ? LocationSummary(tile.Location) : null,
                armyCount = tile.GetAllArmies().Count,
                armies = tile.GetAllArmies().Select(ArmySummary).ToArray()
            };
        }

        static object[] AdjacentActionSummaries(World world, List<Army> armies, Tile origin)
        {
            var actions = new List<object>();
            var map = world.Map;

            for (var y = origin.Y - 1; y <= origin.Y + 1; y++)
            {
                for (var x = origin.X - 1; x <= origin.X + 1; x++)
                {
                    if (x == origin.X && y == origin.Y)
                        continue;

                    if (x < map.GetLowerBound(0) || x > map.GetUpperBound(0) ||
                        y < map.GetLowerBound(1) || y > map.GetUpperBound(1))
                        continue;

                    var target = map[x, y];
                    var action = AdjacentActionSummary(armies, target);
                    actions.Add(action);
                }
            }

            return actions.ToArray();
        }

        static object AdjacentActionSummary(List<Army> armies, Tile target)
        {
            var canTraverse = SafeBool(() => target.CanTraverseHere(armies));
            var canAttack = SafeBool(() => target.CanAttackHere(armies));
            var hasSufficientMoves = SafeBool(() =>
            {
                var armiesWithMoves = Game.Current.MovementCoordinator.GetArmiesWithApplicableMoves(armies, target);
                return Game.Current.MovementCoordinator.HasSufficientMovesAdjacentTile(armiesWithMoves, target);
            });

            var kind = "blocked";
            var legal = false;
            var reason = "Target tile is not traversable for the selected stack.";

            if (canAttack && hasSufficientMoves)
            {
                legal = true;
                kind = target.HasArmies() ? "attack" : "capture-city";
                reason = target.HasArmies()
                    ? "Target contains hostile armies."
                    : "Target contains a non-friendly city that can be attacked or captured.";
            }
            else if (canTraverse && hasSufficientMoves)
            {
                legal = true;
                kind = "move";
                reason = "Target is traversable and the selected stack has enough moves.";
            }
            else if (canTraverse)
            {
                reason = "Target is traversable but selected stack lacks sufficient moves.";
            }

            return new
            {
                kind,
                legal,
                reason,
                target = TileSummary(target)
            };
        }

        static bool SafeBool(Func<bool> evaluate)
        {
            try
            {
                return evaluate();
            }
            catch
            {
                return false;
            }
        }

        static int CountAdjacentEnemyStacks(World world, Player player)
        {
            var count = 0;
            foreach (var army in player.GetArmies().Where(army => !army.IsDead && army.Tile != null))
            {
                var map = world.Map;
                for (var y = army.Tile.Y - 1; y <= army.Tile.Y + 1; y++)
                {
                    for (var x = army.Tile.X - 1; x <= army.Tile.X + 1; x++)
                    {
                        if (x == army.Tile.X && y == army.Tile.Y)
                            continue;

                        if (x < map.GetLowerBound(0) || x > map.GetUpperBound(0) ||
                            y < map.GetLowerBound(1) || y > map.GetUpperBound(1))
                            continue;

                        var tile = map[x, y];
                        if (tile.GetAllArmies().Any(other => other.Clan != player.Clan))
                            count++;
                    }
                }
            }

            return count;
        }

        static IEnumerable<IBid> SafeBids(ITacticalModule module, World world)
        {
            try
            {
                return module.GenerateBids(world) ?? Enumerable.Empty<IBid>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WismUnity AI preview skipped {module.GetType().Name}: {ex.Message}");
                return Enumerable.Empty<IBid>();
            }
        }

        static object BidSummary(IBid bid)
        {
            return new
            {
                module = bid.Module.GetType().Name,
                utility = Math.Round(bid.Utility, 4),
                stack = StackSummary(bid.Armies),
                primaryArmy = bid.Armies != null && bid.Armies.Count > 0 ? ArmySummary(bid.Armies[0]) : null
            };
        }

        sealed class WorldBuilderData
        {
            public object ActiveScene { get; set; }
            public bool SceneReady { get; set; }
            public bool SceneDirty { get; set; }
            public string ProjectRoot { get; set; }
            public string WorldName { get; set; }
            public string ModPath { get; set; }
            public SceneObjectSummary UnityManager { get; set; } = SceneObjectSummary.Missing();
            public SceneObjectSummary WorldTilemap { get; set; } = SceneObjectSummary.Missing();
            public SceneObjectSummary Cities { get; set; } = SceneObjectSummary.Missing();
            public SceneObjectSummary Locations { get; set; } = SceneObjectSummary.Missing();
            public TilemapData Tilemap { get; set; } = new TilemapData();
            public ContainerEntries CityEntries { get; set; } = new ContainerEntries();
            public ContainerEntries LocationEntries { get; set; } = new ContainerEntries();
            public EditorToggle[] EditorToggles { get; set; } = Array.Empty<EditorToggle>();
            public ModWorldData ModWorld { get; set; } = new ModWorldData();

            public object ToSummaryObject()
            {
                return new
                {
                    readOnly = true,
                    activeScene = ActiveScene,
                    sceneReady = SceneReady,
                    worldName = WorldName,
                    modPath = ModPath,
                    requiredObjects = new
                    {
                        unityManager = UnityManager.ToObject(),
                        worldTilemap = WorldTilemap.ToObject(),
                        cities = Cities.ToObject(),
                        locations = Locations.ToObject()
                    },
                    tilemap = Tilemap.ToObject(),
                    cities = CityEntries.ToObject(),
                    locations = LocationEntries.ToObject(),
                    editorToggles = EditorToggles.Select(toggle => toggle.ToObject()).ToArray(),
                    modWorld = ModWorld.ToObject(),
                    timestampUtc = DateTime.UtcNow.ToString("O")
                };
            }
        }

        sealed class SceneObjectSummary
        {
            public bool Found { get; set; }
            public GameObject GameObject { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public string Tag { get; set; }
            public bool ActiveInHierarchy { get; set; }
            public string[] ComponentTypes { get; set; } = Array.Empty<string>();

            public static SceneObjectSummary Missing()
            {
                return new SceneObjectSummary { Found = false };
            }

            public object ToObject()
            {
                return new
                {
                    found = Found,
                    name = Name,
                    path = Path,
                    tag = Tag,
                    activeInHierarchy = ActiveInHierarchy,
                    componentTypes = ComponentTypes
                };
            }
        }

        sealed class TilemapData
        {
            public bool HasTilemapComponent { get; set; }
            public int[] Origin { get; set; } = Array.Empty<int>();
            public int[] Size { get; set; } = Array.Empty<int>();
            public int[] Min { get; set; } = Array.Empty<int>();
            public int[] Max { get; set; } = Array.Empty<int>();
            public int TileCount { get; set; }

            public object ToObject()
            {
                return new
                {
                    hasTilemapComponent = HasTilemapComponent,
                    origin = Origin,
                    size = Size,
                    min = Min,
                    max = Max,
                    tileCount = TileCount
                };
            }
        }

        sealed class ContainerEntries
        {
            public bool Found { get; set; }
            public int DirectChildCount { get; set; }
            public int EntryComponentCount { get; set; }
            public int ObjectsMissingEntryComponent { get; set; }
            public int MissingShortNameCount { get; set; }
            public int SerializedTotal { get; set; } = -1;
            public List<string> ShortNames { get; } = new List<string>();
            public List<SceneEntry> Entries { get; } = new List<SceneEntry>();
            public DuplicateShortName[] DuplicateShortNames { get; set; } = Array.Empty<DuplicateShortName>();

            public object ToObject()
            {
                return new
                {
                    found = Found,
                    directChildCount = DirectChildCount,
                    entryComponentCount = EntryComponentCount,
                    objectsMissingEntryComponent = ObjectsMissingEntryComponent,
                    missingShortNameCount = MissingShortNameCount,
                    serializedTotal = SerializedTotal,
                    duplicateShortNames = DuplicateShortNames.Select(duplicate => duplicate.ToObject()).ToArray(),
                    shortNames = ShortNames.Where(name => !string.IsNullOrWhiteSpace(name)).OrderBy(name => name).ToArray(),
                    entries = Entries.Select(entry => entry.ToObject()).ToArray()
                };
            }
        }

        sealed class SceneEntry
        {
            public string Path { get; set; }
            public string ShortName { get; set; }
            public float[] Position { get; set; } = Array.Empty<float>();

            public object ToObject()
            {
                return new
                {
                    path = Path,
                    shortName = ShortName,
                    position = Position
                };
            }
        }

        sealed class DuplicateShortName
        {
            public string ShortName { get; set; }
            public int Count { get; set; }

            public object ToObject()
            {
                return new
                {
                    shortName = ShortName,
                    count = Count
                };
            }
        }

        sealed class EditorToggle
        {
            public string Container { get; set; }
            public string Name { get; set; }
            public bool Enabled { get; set; }

            public object ToObject()
            {
                return new
                {
                    container = Container,
                    name = Name,
                    enabled = Enabled
                };
            }
        }

        sealed class ModWorldData
        {
            public List<ModWorldCandidate> Candidates { get; } = new List<ModWorldCandidate>();
            public bool AnyWorldDirectoryFound { get; set; }
            public bool AnyCityJsonFound { get; set; }
            public bool AnyLocationJsonFound { get; set; }
            public bool AnyMapJsonFound { get; set; }
            public int CityCount { get; set; } = -1;
            public int LocationCount { get; set; } = -1;
            public int MapTileCount { get; set; } = -1;
            public string[] CityShortNames { get; set; } = Array.Empty<string>();
            public string[] LocationShortNames { get; set; } = Array.Empty<string>();

            public object ToObject()
            {
                return new
                {
                    anyWorldDirectoryFound = AnyWorldDirectoryFound,
                    anyCityJsonFound = AnyCityJsonFound,
                    anyLocationJsonFound = AnyLocationJsonFound,
                    anyMapJsonFound = AnyMapJsonFound,
                    cityCount = CityCount,
                    locationCount = LocationCount,
                    mapTileCount = MapTileCount,
                    cityShortNameCount = CityShortNames.Length,
                    locationShortNameCount = LocationShortNames.Length,
                    candidates = Candidates.Select(candidate => candidate.ToObject()).ToArray()
                };
            }
        }

        sealed class ModWorldCandidate
        {
            public string Path { get; set; }
            public bool Exists { get; set; }
            public JsonFileSummary CityJson { get; set; } = new JsonFileSummary();
            public JsonFileSummary LocationJson { get; set; } = new JsonFileSummary();
            public JsonFileSummary MapJson { get; set; } = new JsonFileSummary();

            public object ToObject()
            {
                return new
                {
                    path = Path,
                    exists = Exists,
                    cityJson = CityJson.ToObject(),
                    locationJson = LocationJson.ToObject(),
                    mapJson = MapJson.ToObject()
                };
            }
        }

        sealed class JsonFileSummary
        {
            public string Path { get; set; }
            public bool Exists { get; set; }
            public long Bytes { get; set; }
            public int ObjectCount { get; set; } = -1;
            public string[] ShortNames { get; set; } = Array.Empty<string>();

            public object ToObject()
            {
                return new
                {
                    path = Path,
                    exists = Exists,
                    bytes = Bytes,
                    objectCount = ObjectCount,
                    shortNameCount = ShortNames.Length
                };
            }
        }

        sealed class WorldBuilderIssue
        {
            public string Severity { get; set; }
            public string Code { get; set; }
            public string Message { get; set; }

            public static WorldBuilderIssue Error(string code, string message)
            {
                return new WorldBuilderIssue { Severity = "Error", Code = code, Message = message };
            }

            public static WorldBuilderIssue Warning(string code, string message)
            {
                return new WorldBuilderIssue { Severity = "Warning", Code = code, Message = message };
            }

            public static WorldBuilderIssue Info(string code, string message)
            {
                return new WorldBuilderIssue { Severity = "Info", Code = code, Message = message };
            }

            public object ToObject()
            {
                return new
                {
                    severity = Severity,
                    code = Code,
                    message = Message
                };
            }
        }

        sealed class ValidationSnapshot
        {
            public string ScenePath { get; set; }
            public string WorldName { get; set; }
            public string Status { get; set; }
            public int Errors { get; set; }
            public int Warnings { get; set; }
            public int CitySceneCount { get; set; }
            public int LocationSceneCount { get; set; }
            public int CityModCount { get; set; } = -1;
            public int LocationModCount { get; set; } = -1;
            public int TileCount { get; set; }
            public WorldBuilderIssue[] Issues { get; set; } = Array.Empty<WorldBuilderIssue>();
        }
    }
}
