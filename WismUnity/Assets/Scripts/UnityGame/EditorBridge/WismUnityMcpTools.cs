using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    }
}
