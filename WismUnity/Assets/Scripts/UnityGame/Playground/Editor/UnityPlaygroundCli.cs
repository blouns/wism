using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UnityGame.ModKit;
using Assets.Scripts.UnityGame.Persistance.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wism.Client.Commands;
using Wism.Client.Core;

namespace WismUnity.Playground
{
    public static class UnityPlaygroundCli
    {
        const string DefaultWorld = "TestWorld";
        const string DefaultScene = "Assets/Scenes/Test/TestWorld.unity";
        static EditorWindow batchGameView;
        static ScriptableObject batchHostView;

        public static void Run()
        {
            var exitCode = 0;
            var options = UnityPlaygroundOptions.FromCommandLine(Environment.GetCommandLineArgs());
            var report = new UnityPlaygroundReport
            {
                schemaVersion = 1,
                command = options.Command,
                profile = options.Profile,
                packs = options.Packs,
                world = options.World,
                modRoot = options.ModRoot,
                scenePath = options.ScenePath,
                scenarioName = options.Scenario,
                runId = options.RunId,
                startedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                batchMode = Application.isBatchMode,
                artifactDirectory = options.OutputDirectory
            };

            try
            {
                Directory.CreateDirectory(options.OutputDirectory);
                if (string.Equals(options.Command, "modkit-status", StringComparison.OrdinalIgnoreCase))
                {
                    RunModKitStatus(options, report);
                }
                else
                {
                    RunWorldSmoke(options, report);
                }

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

        static void RunModKitStatus(UnityPlaygroundOptions options, UnityPlaygroundReport report)
        {
            report.selection = UnityModKitSelection.Inspect(
                options.Profile,
                options.Packs,
                options.World,
                options.ModRoot);
            report.profile = report.selection.profileId;
            report.packs = report.selection.activePackIds.Length > 0
                ? report.selection.activePackIds
                : report.selection.requestedPackIds;
            report.world = report.selection.worldName;
            report.modRoot = report.selection.modRoot;
            report.scene = SceneSummary(EditorSceneManager.GetActiveScene());
            report.console = ConsoleSummary();
            report.dirtyScenes = GetLoadedDirtyScenes();
            report.status = string.Equals(report.selection.status, "Failed", StringComparison.OrdinalIgnoreCase)
                ? "Failed"
                : "Passed";
            report.outcome = "Generated read-only Mod Kit status report without loading or saving scenes.";
            report.events.Add("Generated read-only Mod Kit status report.");
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

                report.selection = UnityModKitSelection.Apply(
                    unityManager,
                    options.Profile,
                    options.Packs,
                    options.World,
                    options.ModRoot);
                report.profile = report.selection.profileId;
                report.packs = report.selection.activePackIds;
                report.world = report.selection.worldName;
                report.modRoot = report.selection.modRoot;
                report.events.Add(report.selection.outcome);

                UnityManager.SetNewGameSettings(CreateSettings(options, report.selection.worldName, report.selection.seed, report.selection.selectionEntity));
                unityManager.Initialize(CreateSettings(options, report.selection.worldName, report.selection.seed, report.selection.selectionEntity));
                report.events.Add("Initialized UnityManager with deterministic playground settings.");

                if (options.AdvanceBootstrap)
                {
                    AdvanceTicks(unityManager, 2);
                    report.events.Add("Advanced UnityManager bootstrap with two FixedUpdate calls.");
                }

                RunScenario(options, report, unityManager);
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

                if (!string.Equals(report.status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    report.status = "Passed";
                    report.outcome = report.scenario != null &&
                                     !string.Equals(report.scenario.name, "smoke", StringComparison.OrdinalIgnoreCase)
                        ? report.scenario.outcome
                        : $"{options.World} loaded and initialized without dirtying loaded scenes.";
                }
            }
            finally
            {
                if (originalSetup.Length > 0)
                {
                    report.events.Add("Restoring original editor scene setup without saving.");
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
                else if (scene.IsValid() && scene.isLoaded)
                {
                    report.events.Add("No original editor scene setup was loaded; closing playground scene without saving.");
                    EditorSceneManager.CloseScene(scene, true);
                }
                else
                {
                    report.events.Add("No original editor scene setup was loaded; leaving batchmode scene cleanup to Unity shutdown.");
                }
            }
        }

        static void RunScenario(
            UnityPlaygroundOptions options,
            UnityPlaygroundReport report,
            UnityManager unityManager)
        {
            var scenarioName = string.IsNullOrWhiteSpace(options.Scenario)
                ? "smoke"
                : options.Scenario.Trim();
            if (string.Equals(scenarioName, "smoke", StringComparison.OrdinalIgnoreCase))
            {
                report.scenario = new UnityPlaygroundScenarioSummary
                {
                    name = "smoke",
                    status = "Passed",
                    outcome = "Scene and UnityManager initialization only.",
                    maxTicks = 0,
                    ticksRun = 0,
                    startLastCommandId = unityManager.LastCommandId,
                    endLastCommandId = unityManager.LastCommandId,
                    queuedCommandCount = 0,
                    executedCommandCount = 0,
                    startingClan = CurrentClanName(),
                    endingClan = CurrentClanName()
                };
                return;
            }

            if (string.Equals(scenarioName, "turn-cycle-smoke", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scenarioName, "end-turn-smoke", StringComparison.OrdinalIgnoreCase))
            {
                RunTurnCycleSmoke(options, report, unityManager, scenarioName);
                return;
            }

            if (string.Equals(scenarioName, "save-load-smoke", StringComparison.OrdinalIgnoreCase))
            {
                RunSaveLoadSmoke(options, report, unityManager);
                return;
            }

            if (string.Equals(scenarioName, "viewport-input-proof", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scenarioName, "resolution-input-proof", StringComparison.OrdinalIgnoreCase))
            {
                RunViewportInputProof(options, report, unityManager);
                return;
            }

            if (string.Equals(scenarioName, "mixed-human-ai-marathon", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scenarioName, "mixed-mode", StringComparison.OrdinalIgnoreCase))
            {
                RunMixedHumanAiMarathon(options, report, unityManager);
                return;
            }

            throw new InvalidOperationException($"Unknown Unity Playground scenario: {scenarioName}");
        }

        static void RunMixedHumanAiMarathon(
            UnityPlaygroundOptions options,
            UnityPlaygroundReport report,
            UnityManager unityManager)
        {
            var bootstrapTicks = EnsureRunning(unityManager);
            if (bootstrapTicks > 0)
            {
                report.events.Add($"Advanced UnityManager to Running with {bootstrapTicks} bootstrap tick(s).");
            }

            var startLastCommandId = unityManager.LastCommandId;
            var startingClan = CurrentClanName();
            report.screenshots.Add(new UnityPlaygroundScreenshotEntry
            {
                label = "start",
                path = CaptureScreenshot(options.OutputDirectory, options.RunId + "-start")
            });

            var scriptedHumanTurns = 0;
            var humanDecisionsApplied = 0;
            var humanDecisionFallbacks = 0;
            var aiTurnsObserved = 0;
            var commandStalls = 0;
            var ticksRun = 0;
            var completedTurns = 0;
            var lastCommandId = unityManager.LastCommandId;
            var lastClan = CurrentClanName();
            var stuckCommandId = 0;
            var stuckCommandType = string.Empty;
            var humanDecisionScript = UnityPlaygroundHumanDecisionScript.Load(options.HumanDecisionScriptPath);
            if (humanDecisionScript.available)
            {
                report.events.Add($"Loaded human decision script: {options.HumanDecisionScriptPath}");
            }

            while (ticksRun < options.MaxTicks && completedTurns < options.Turns)
            {
                var player = Game.Current.GetCurrentPlayer();
                if (player.IsHuman && !HasQueuedCommand(unityManager))
                {
                    var applied = ApplyHumanDecision(unityManager, player, humanDecisionScript, report, out var endedTurn);
                    humanDecisionsApplied += applied ? 1 : 0;
                    if (!applied)
                    {
                        humanDecisionFallbacks++;
                        unityManager.GameManager.EndTurn();
                        endedTurn = true;
                        report.events.Add($"Fallback human agent ended turn for {player.Clan.ShortName}.");
                    }

                    if (endedTurn)
                    {
                        scriptedHumanTurns++;
                    }
                }
                else if (!player.IsHuman)
                {
                    aiTurnsObserved++;
                }

                unityManager.FixedUpdate();
                ticksRun++;

                var currentClan = CurrentClanName();
                if (!string.Equals(currentClan, lastClan, StringComparison.OrdinalIgnoreCase))
                {
                    completedTurns++;
                    lastClan = currentClan;
                }

                if (unityManager.LastCommandId == lastCommandId && !HasQueuedCommand(unityManager))
                {
                    commandStalls++;
                    if (commandStalls > options.MaxCommandStalls)
                    {
                        report.events.Add($"Stopping mixed-mode run after {commandStalls} idle command stall tick(s).");
                        break;
                    }
                }
                else if (unityManager.LastCommandId == lastCommandId)
                {
                    commandStalls++;
                    var stuckCommand = GetNextCommand(unityManager);
                    if (stuckCommand != null)
                    {
                        stuckCommandId = stuckCommand.Id;
                        stuckCommandType = stuckCommand.GetType().Name;
                    }

                    if (commandStalls > options.MaxCommandStalls)
                    {
                        report.events.Add($"Stopping mixed-mode run after {commandStalls} queued command stall tick(s) at {stuckCommandType}#{stuckCommandId}.");
                        break;
                    }
                }
                else
                {
                    commandStalls = 0;
                    lastCommandId = unityManager.LastCommandId;
                    stuckCommandId = 0;
                    stuckCommandType = string.Empty;
                }
            }

            report.screenshots.Add(new UnityPlaygroundScreenshotEntry
            {
                label = "end",
                path = CaptureScreenshot(options.OutputDirectory, options.RunId + "-end")
            });

            var commands = GetCommandsAfterId(unityManager, startLastCommandId).ToArray();
            report.commandTrace.AddRange(commands.Select(command => CommandTraceEntry(command, unityManager.LastCommandId)));
            report.invariants.AddRange(CollectInvariants());

            var failedInvariants = report.invariants
                .Where(item => string.Equals(item.status, "Failed", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var executedCount = commands.Count(command => command.Id <= unityManager.LastCommandId);
            var humanClans = Game.Current.Players.Where(player => player.IsHuman).Select(player => player.Clan.ShortName).ToArray();
            var aiClans = Game.Current.Players.Where(player => !player.IsHuman).Select(player => player.Clan.ShortName).ToArray();
            var passed = completedTurns >= options.Turns &&
                         commandStalls <= options.MaxCommandStalls &&
                         failedInvariants.Length == 0;

            report.mixedMode = new UnityPlaygroundMixedModeSummary
            {
                seed = ResolveSeed(options),
                fuzz = options.Fuzz,
                humanAgentCount = humanClans.Length,
                aiAgentCount = aiClans.Length,
                turnsRequested = options.Turns,
                turnsCompleted = completedTurns,
                scriptedHumanTurns = scriptedHumanTurns,
                aiTurnsObserved = aiTurnsObserved,
                commandStalls = commandStalls,
                humanDecisionsApplied = humanDecisionsApplied,
                humanDecisionFallbacks = humanDecisionFallbacks,
                cityCaptures = CountCommand(commands, "CaptureCity"),
                searches = CountCommand(commands, "Search"),
                battles = CountCommand(commands, "Battle") + CountCommand(commands, "Attack"),
                stuckCommandId = stuckCommandId,
                stuckCommandType = stuckCommandType,
                humanDecisionScriptPath = options.HumanDecisionScriptPath,
                humanClans = humanClans,
                aiClans = aiClans
            };
            report.scenario = new UnityPlaygroundScenarioSummary
            {
                name = "mixed-human-ai-marathon",
                status = passed ? "Passed" : "Failed",
                outcome = passed
                    ? "Completed a mixed human-agent and AI-agent turn bridge through Unity FixedUpdate."
                    : "Mixed-mode run did not satisfy turn, stall, or invariant gates.",
                maxTicks = options.MaxTicks,
                ticksRun = ticksRun,
                startLastCommandId = startLastCommandId,
                endLastCommandId = unityManager.LastCommandId,
                queuedCommandCount = commands.Length,
                executedCommandCount = executedCount,
                startingClan = startingClan,
                endingClan = CurrentClanName()
            };

            report.events.Add($"mixed-human-ai-marathon: turns={completedTurns}/{options.Turns}, commands={executedCount}, invariants failed={failedInvariants.Length}.");
            if (!passed)
            {
                report.status = "Failed";
                report.outcome = report.scenario.outcome;
            }
        }

        static bool ApplyHumanDecision(
            UnityManager unityManager,
            Player player,
            UnityPlaygroundHumanDecisionScript script,
            UnityPlaygroundReport report,
            out bool endedTurn)
        {
            endedTurn = false;
            var decision = script.Next(player.Clan.ShortName);
            if (decision == null)
            {
                return false;
            }

            try
            {
                var action = (decision.action ?? string.Empty).Trim();
                if (string.Equals(action, "endTurn", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(action, "end-turn", StringComparison.OrdinalIgnoreCase))
                {
                    unityManager.GameManager.EndTurn();
                    endedTurn = true;
                    report.events.Add($"Human decision {player.Clan.ShortName}: endTurn.");
                    return true;
                }

                if (string.Equals(action, "selectNextArmy", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(action, "select-next-army", StringComparison.OrdinalIgnoreCase))
                {
                    unityManager.GameManager.SelectNextArmy();
                    report.events.Add($"Human decision {player.Clan.ShortName}: selectNextArmy.");
                    return true;
                }

                if (string.Equals(action, "deselect", StringComparison.OrdinalIgnoreCase))
                {
                    unityManager.GameManager.DeselectArmies();
                    report.events.Add($"Human decision {player.Clan.ShortName}: deselect.");
                    return true;
                }

                if (string.Equals(action, "defend", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Game.Current.ArmiesSelected())
                    {
                        return false;
                    }

                    unityManager.GameManager.DefendSelectedArmies();
                    report.events.Add($"Human decision {player.Clan.ShortName}: defend.");
                    return true;
                }

                if (string.Equals(action, "search", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Game.Current.ArmiesSelected())
                    {
                        return false;
                    }

                    unityManager.GameManager.SearchLocation();
                    report.events.Add($"Human decision {player.Clan.ShortName}: search.");
                    return true;
                }

                if (string.Equals(action, "moveSelected", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(action, "move-selected", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Game.Current.ArmiesSelected() || !HasCoordinates(decision))
                    {
                        return false;
                    }

                    unityManager.GameManager.MoveSelectedArmies(decision.x, decision.y);
                    report.events.Add($"Human decision {player.Clan.ShortName}: moveSelected {decision.x},{decision.y}.");
                    return true;
                }

                if (string.Equals(action, "attackSelected", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(action, "attack-selected", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Game.Current.ArmiesSelected() || !HasCoordinates(decision))
                    {
                        return false;
                    }

                    unityManager.GameManager.AttackWithSelectedArmies(decision.x, decision.y);
                    report.events.Add($"Human decision {player.Clan.ShortName}: attackSelected {decision.x},{decision.y}.");
                    return true;
                }

                report.events.Add($"Human decision {player.Clan.ShortName}: unsupported action '{action}'.");
                return false;
            }
            catch (Exception ex)
            {
                report.events.Add($"Human decision {player.Clan.ShortName} failed: {ex.Message}");
                return false;
            }
        }

        static bool HasCoordinates(UnityPlaygroundHumanDecision decision)
        {
            return decision.x >= 0 && decision.y >= 0;
        }

        static void RunSaveLoadSmoke(
            UnityPlaygroundOptions options,
            UnityPlaygroundReport report,
            UnityManager unityManager)
        {
            var bootstrapTicks = EnsureRunning(unityManager);
            if (bootstrapTicks > 0)
            {
                report.events.Add($"Advanced UnityManager to Running with {bootstrapTicks} bootstrap tick(s).");
            }

            var filename = options.RunId + ".json";
            PersistanceManager.Save(filename, "Unity Mod Kit save/load smoke", unityManager);
            var savePath = Path.Combine(Application.persistentDataPath, filename);
            var snapshot = PersistanceManager.LoadEntities(savePath, unityManager);
            var savedSelection = snapshot.ModKitSelection ?? snapshot.WismGameEntity?.ModKitSelection;
            var loadedReport = UnityModKitSelection.ApplySavedSelection(
                unityManager,
                savedSelection,
                UnityModKitSelection.PluginModRoot);

            var expectedFingerprint = report.selection?.selectionEntity?.ContentFingerprint ?? string.Empty;
            var actualFingerprint = savedSelection?.ContentFingerprint ?? string.Empty;
            var passed = savedSelection != null &&
                         string.Equals(expectedFingerprint, actualFingerprint, StringComparison.OrdinalIgnoreCase) &&
                         loadedReport.isLoadable;

            report.scenario = new UnityPlaygroundScenarioSummary
            {
                name = "save-load-smoke",
                status = passed ? "Passed" : "Failed",
                outcome = passed
                    ? "Saved and reloaded a Unity game with matching Mod Kit selection metadata and fingerprint."
                    : "Saved game did not retain a loadable matching Mod Kit selection.",
                maxTicks = options.MaxTicks,
                ticksRun = bootstrapTicks,
                startLastCommandId = 0,
                endLastCommandId = unityManager.LastCommandId,
                queuedCommandCount = 0,
                executedCommandCount = 0,
                startingClan = CurrentClanName(),
                endingClan = CurrentClanName()
            };
            report.events.Add("Save/load smoke file: " + savePath);

            if (!passed)
            {
                report.status = "Failed";
                report.outcome = report.scenario.outcome;
            }
        }

        static void RunTurnCycleSmoke(
            UnityPlaygroundOptions options,
            UnityPlaygroundReport report,
            UnityManager unityManager,
            string scenarioName)
        {
            var startingClan = CurrentClanName();
            var startLastCommandId = unityManager.LastCommandId;

            var bootstrapTicks = EnsureRunning(unityManager);
            if (bootstrapTicks > 0)
            {
                report.events.Add($"Advanced UnityManager to Running with {bootstrapTicks} bootstrap tick(s).");
            }

            var queuedBefore = CountCommandsAfterId(unityManager, startLastCommandId);
            unityManager.GameManager.EndTurn();
            var queuedAfter = GetCommandsAfterId(unityManager, startLastCommandId).ToArray();
            var targetCommandId = queuedAfter.Length > 0
                ? queuedAfter.Max(command => command.Id)
                : startLastCommandId;

            var ticksRun = 0;
            while (ticksRun < options.MaxTicks && unityManager.LastCommandId < targetCommandId)
            {
                unityManager.FixedUpdate();
                ticksRun++;
            }

            var commands = GetCommandsAfterId(unityManager, startLastCommandId).ToArray();
            report.commandTrace.AddRange(commands.Select(command => CommandTraceEntry(command, unityManager.LastCommandId)));

            var executedCount = commands.Count(command => command.Id <= unityManager.LastCommandId);
            var passed = unityManager.LastCommandId >= targetCommandId &&
                         executedCount >= queuedAfter.Length &&
                         queuedAfter.Length > queuedBefore;
            report.scenario = new UnityPlaygroundScenarioSummary
            {
                name = scenarioName,
                status = passed ? "Passed" : "Failed",
                outcome = passed
                    ? "Queued and executed a bounded end-turn command sequence through Unity FixedUpdate."
                    : "End-turn command sequence did not complete within the tick budget.",
                maxTicks = options.MaxTicks,
                ticksRun = ticksRun,
                startLastCommandId = startLastCommandId,
                endLastCommandId = unityManager.LastCommandId,
                queuedCommandCount = Math.Max(0, queuedAfter.Length - queuedBefore),
                executedCommandCount = executedCount,
                startingClan = startingClan,
                endingClan = CurrentClanName()
            };

            report.events.Add($"{scenarioName}: queued {report.scenario.queuedCommandCount} command(s), executed {executedCount} command(s) in {ticksRun} tick(s).");
            if (!passed)
            {
                report.status = "Failed";
                report.outcome = report.scenario.outcome;
            }
        }

        static void RunViewportInputProof(
            UnityPlaygroundOptions options,
            UnityPlaygroundReport report,
            UnityManager unityManager)
        {
            var requestedWidth = options.ScreenWidth > 0 ? options.ScreenWidth : Screen.width;
            var requestedHeight = options.ScreenHeight > 0 ? options.ScreenHeight : Screen.height;
            var gameViewSizeApplied = false;
            var gameViewSizeEvidence = "No explicit screen target requested.";
            var selectedGameViewWidth = 0;
            var selectedGameViewHeight = 0;
            var selectedGameViewEvidence = "Selected Editor GameView size was not inspected.";
            if (options.ScreenWidth > 0 && options.ScreenHeight > 0)
            {
                Screen.SetResolution(options.ScreenWidth, options.ScreenHeight, false);
                gameViewSizeApplied = TryApplyEditorGameViewSize(
                    options.ScreenWidth,
                    options.ScreenHeight,
                    string.IsNullOrWhiteSpace(options.ScreenLabel) ? options.RunId : options.ScreenLabel,
                    out gameViewSizeEvidence);
                TryReadSelectedEditorGameViewSize(out selectedGameViewWidth, out selectedGameViewHeight, out selectedGameViewEvidence);
                report.events.Add($"Requested Game view resolution {options.ScreenWidth}x{options.ScreenHeight}.");
                report.events.Add(gameViewSizeEvidence);
                report.events.Add(selectedGameViewEvidence);
            }

            var sample = CollectViewportInputSample(
                unityManager.gameObject.scene,
                options,
                requestedWidth,
                requestedHeight,
                gameViewSizeApplied,
                gameViewSizeEvidence,
                selectedGameViewWidth,
                selectedGameViewHeight,
                selectedGameViewEvidence);
            var passed = sample.uiPanel != null &&
                         sample.uiPanel.found &&
                         sample.uiPanel.active &&
                         sample.uiPanel.width > 0f &&
                         sample.uiPanel.height > 0f &&
                         string.Equals(sample.screenToMap.status, "Passed", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(sample.minimap.status, "Passed", StringComparison.OrdinalIgnoreCase) &&
                         (!options.RequireActualResolution ||
                          sample.actualResolutionMatchesRequested ||
                          sample.selectedGameViewSizeMatchesRequested);

            if (options.CaptureScreenshot)
            {
                sample.screenshotPath = CaptureScreenshot(options.OutputDirectory, options.RunId + "-" + SafeFileName(sample.label));
                report.screenshots.Add(new UnityPlaygroundScreenshotEntry
                {
                    label = sample.label,
                    path = sample.screenshotPath
                });
            }

            report.viewportInputProof = new UnityPlaygroundViewportInputProof
            {
                status = passed ? "Passed" : "Failed",
                outcome = passed
                    ? "Viewport, UI panel, screen-to-map, and minimap coordinate proofs passed for the requested target."
                    : "One or more viewport, UI, screen-to-map, or minimap coordinate proofs failed.",
                supportedTargets = SupportedViewportTargets(),
                fixedSizeAssumptions = FixedSizeAssumptions(),
                sample = sample
            };
            report.scenario = new UnityPlaygroundScenarioSummary
            {
                name = "viewport-input-proof",
                status = passed ? "Passed" : "Failed",
                outcome = report.viewportInputProof.outcome,
                maxTicks = options.MaxTicks,
                ticksRun = 0,
                startLastCommandId = unityManager.LastCommandId,
                endLastCommandId = unityManager.LastCommandId,
                queuedCommandCount = 0,
                executedCommandCount = 0,
                startingClan = CurrentClanName(),
                endingClan = CurrentClanName()
            };

            report.events.Add($"viewport-input-proof {sample.label}: requested={sample.requestedWidth}x{sample.requestedHeight}, actual={sample.actualWidth}x{sample.actualHeight}, actualMatch={sample.actualResolutionMatchesRequested}, selectedGameView={sample.selectedGameViewWidth}x{sample.selectedGameViewHeight}, selectedMatch={sample.selectedGameViewSizeMatchesRequested}, minimap={sample.minimap.status}, screenToMap={sample.screenToMap.status}.");
            if (!passed)
            {
                report.status = "Failed";
                report.outcome = report.scenario.outcome;
            }
        }

        static UnityPlaygroundViewportSample CollectViewportInputSample(
            Scene scene,
            UnityPlaygroundOptions options,
            int requestedWidth,
            int requestedHeight,
            bool gameViewSizeApplied,
            string gameViewSizeEvidence,
            int selectedGameViewWidth,
            int selectedGameViewHeight,
            string selectedGameViewEvidence)
        {
            var label = string.IsNullOrWhiteSpace(options.ScreenLabel)
                ? $"{requestedWidth}x{requestedHeight}"
                : options.ScreenLabel;
            var mainCamera = FindSceneObject(scene, "MainCamera")?.GetComponent<Camera>();
            var tilemap = FindSceneObject(scene, "WorldTilemap")?.GetComponent<WorldTilemap>();
            if (tilemap != null)
            {
                tilemap.Start();
            }

            var world = TryGetCurrentWorld();
            var mapWidth = world?.Map?.GetLength(0) ?? 0;
            var mapHeight = world?.Map?.GetLength(1) ?? 0;
            return new UnityPlaygroundViewportSample
            {
                label = label,
                requestedWidth = requestedWidth,
                requestedHeight = requestedHeight,
                actualWidth = Screen.width,
                actualHeight = Screen.height,
                actualResolutionMatchesRequested = Screen.width == requestedWidth && Screen.height == requestedHeight,
                selectedGameViewSizeMatchesRequested = selectedGameViewWidth == requestedWidth && selectedGameViewHeight == requestedHeight,
                selectedGameViewWidth = selectedGameViewWidth,
                selectedGameViewHeight = selectedGameViewHeight,
                selectedGameViewEvidence = selectedGameViewEvidence,
                gameViewSizeApplied = gameViewSizeApplied,
                gameViewSizeEvidence = gameViewSizeEvidence,
                resolutionEvidence = Screen.width == requestedWidth && Screen.height == requestedHeight
                    ? $"Actual Game view resolution matched requested {requestedWidth}x{requestedHeight}."
                    : $"Actual Game view resolution was {Screen.width}x{Screen.height} after requested {requestedWidth}x{requestedHeight}.",
                cameraAspect = mainCamera != null ? mainCamera.aspect : 0f,
                cameraOrthographicSize = mainCamera != null ? mainCamera.orthographicSize : 0f,
                cursorScale = CursorManager.CalculateViewportScale(Screen.height, 0.45f, 1f),
                uiPanel = CollectUiPanelProof(scene),
                screenToMap = CollectScreenToMapProof(mainCamera, tilemap),
                minimap = CollectMinimapProof(scene, mapWidth, mapHeight)
            };
        }

        static bool TryApplyEditorGameViewSize(int width, int height, string label, out string evidence)
        {
            evidence = "Editor GameView size reflection was not attempted.";
            if (width <= 0 || height <= 0)
            {
                evidence = "Editor GameView size reflection skipped because requested dimensions were invalid.";
                return false;
            }

            try
            {
                var editorAssembly = typeof(EditorWindow).Assembly;
                var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                var gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                var gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                var gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                if (gameViewType == null ||
                    gameViewSizesType == null ||
                    gameViewSizeType == null ||
                    gameViewSizeTypeEnum == null)
                {
                    evidence = "Editor GameView reflection types were unavailable.";
                    return false;
                }

                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                var instance = singletonType
                    .GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(null);
                var getGroup = gameViewSizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (instance == null || getGroup == null)
                {
                    evidence = "Editor GameView size singleton or group accessor was unavailable.";
                    return false;
                }

                var group = getGroup.Invoke(instance, new object[] { GameViewSizeGroupType.Standalone });
                if (group == null)
                {
                    evidence = "Editor GameView Standalone size group was unavailable.";
                    return false;
                }

                var groupType = group.GetType();
                var getBuiltinCount = groupType.GetMethod("GetBuiltinCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var getCustomCount = groupType.GetMethod("GetCustomCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var addCustomSize = groupType.GetMethod("AddCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var removeCustomSize = groupType.GetMethod("RemoveCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var getGameViewSize = groupType.GetMethod("GetGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (addCustomSize == null || getGameViewSize == null)
                {
                    evidence = "Editor GameView size group mutation methods were unavailable.";
                    return false;
                }

                var builtinCount = getBuiltinCount != null ? (int)getBuiltinCount.Invoke(group, null) : 0;
                var totalCount = builtinCount + (getCustomCount != null ? (int)getCustomCount.Invoke(group, null) : 0);
                for (var index = totalCount - 1; index >= builtinCount; index--)
                {
                    var existingSize = getGameViewSize.Invoke(group, new object[] { index });
                    if (existingSize == null)
                    {
                        continue;
                    }

                    var existingWidth = (int)(gameViewSizeType.GetProperty("width")?.GetValue(existingSize) ?? -1);
                    var existingHeight = (int)(gameViewSizeType.GetProperty("height")?.GetValue(existingSize) ?? -1);
                    if (existingWidth == width && existingHeight == height)
                    {
                        SelectEditorGameViewSize(gameViewType, index, width, height);
                        evidence = $"Selected existing Editor GameView size {width}x{height} at index {index}.";
                        return true;
                    }
                }

                var fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                var customSize = Activator.CreateInstance(gameViewSizeType, fixedResolution, width, height, label);
                addCustomSize.Invoke(group, new[] { customSize });
                var customIndex = builtinCount + (getCustomCount != null ? (int)getCustomCount.Invoke(group, null) : 1) - 1;
                SelectEditorGameViewSize(gameViewType, customIndex, width, height);
                evidence = $"Added and selected Editor GameView fixed resolution {width}x{height} at index {customIndex}.";
                return true;
            }
            catch (Exception ex)
            {
                evidence = $"Editor GameView size reflection failed: {ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        static bool TryReadSelectedEditorGameViewSize(out int width, out int height, out string evidence)
        {
            if (Application.isBatchMode)
            {
                width = Screen.width;
                height = Screen.height;
                evidence = $"Batch render viewport is {width}x{height}; no desktop window requested.";
                return width > 0 && height > 0;
            }
            width = 0;
            height = 0;
            evidence = "Selected Editor GameView size reflection was not attempted.";
            try
            {
                var editorAssembly = typeof(EditorWindow).Assembly;
                var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                var gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                var gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                if (gameViewType == null || gameViewSizesType == null || gameViewSizeType == null)
                {
                    evidence = "Selected Editor GameView reflection types were unavailable.";
                    return false;
                }

                var gameView = EditorWindow.GetWindow(gameViewType);
                var selectedSizeIndex = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var index = selectedSizeIndex != null ? (int)selectedSizeIndex.GetValue(gameView) : -1;
                if (index < 0)
                {
                    evidence = "Selected Editor GameView size index was unavailable.";
                    return false;
                }

                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                var instance = singletonType
                    .GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(null);
                var getGroup = gameViewSizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var group = getGroup?.Invoke(instance, new object[] { GameViewSizeGroupType.Standalone });
                var getGameViewSize = group?.GetType().GetMethod("GetGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var selectedSize = getGameViewSize?.Invoke(group, new object[] { index });
                if (selectedSize == null)
                {
                    evidence = $"Selected Editor GameView size object was unavailable for index {index}.";
                    return false;
                }

                width = (int)(gameViewSizeType.GetProperty("width")?.GetValue(selectedSize) ?? 0);
                height = (int)(gameViewSizeType.GetProperty("height")?.GetValue(selectedSize) ?? 0);
                evidence = $"Selected Editor GameView fixed size reports {width}x{height} at index {index}.";
                return width > 0 && height > 0;
            }
            catch (Exception ex)
            {
                evidence = $"Selected Editor GameView size reflection failed: {ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        static void SelectEditorGameViewSize(Type gameViewType, int index, int width, int height)
        {
            if (Application.isBatchMode)
            {
                if (!TryApplyBatchViewport(index, width, height, out var evidence))
                    throw new InvalidOperationException(evidence);
                return;
            }
            var gameView = EditorWindow.GetWindow(gameViewType);
            var selectedSizeIndex = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selectedSizeIndex?.SetValue(gameView, index);
            gameView.minSize = new Vector2(width, height);
            gameView.position = new Rect(0f, 0f, width, height);
            gameView.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        static bool TryApplyBatchViewport(int index, int width, int height, out string evidence)
        {
            try
            {
                var assembly = typeof(EditorWindow).Assembly;
                var gameType = assembly.GetType("UnityEditor.GameView");
                var hostType = assembly.GetType("UnityEditor.HostView");
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (batchGameView == null)
                {
                    // A render host without a ContainerWindow cannot show or focus a desktop window.
                    batchGameView = (EditorWindow)ScriptableObject.CreateInstance(gameType);
                    batchHostView = ScriptableObject.CreateInstance(hostType);
                    batchGameView.hideFlags = batchHostView.hideFlags = HideFlags.HideAndDontSave;
                    hostType.GetMethod("SetWindow", flags).Invoke(batchHostView, new object[] { null });
                    hostType.GetProperty("actualView", flags).SetValue(batchHostView, batchGameView);
                    EditorApplication.quitting += ReleaseBatchViewport;
                    AssemblyReloadEvents.beforeAssemblyReload += ReleaseBatchViewport;
                }
                gameType.GetProperty("selectedSizeIndex", flags).SetValue(batchGameView, index);
                gameType.BaseType.GetProperty("targetSize", flags).SetValue(batchGameView, new Vector2(width, height));
                hostType.GetMethod("SetMainPlayModeViewSize", flags).Invoke(batchHostView, new object[] { new Vector2(width, height) });
                hostType.GetMethod("SetDisplayViewSize", flags).Invoke(batchHostView, new object[] { 0, new Vector2(width, height) });
                EditorApplication.QueuePlayerLoopUpdate();
                evidence = $"Requested windowless batch viewport {width}x{height}.";
                return true;
            }
            catch (Exception exception)
            {
                ReleaseBatchViewport();
                evidence = $"Windowless viewport unavailable: {exception.GetBaseException().Message}";
                return false;
            }
        }

        static void ReleaseBatchViewport()
        {
            EditorApplication.quitting -= ReleaseBatchViewport;
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseBatchViewport;
            if (batchGameView != null) UnityEngine.Object.DestroyImmediate(batchGameView);
            if (batchHostView != null) UnityEngine.Object.DestroyImmediate(batchHostView);
            batchGameView = null;
            batchHostView = null;
        }

        static UnityPlaygroundUiPanelProof CollectUiPanelProof(Scene scene)
        {
            var panelObject = FindSceneObject(scene, "MinimapPanel");
            var panel = panelObject?.GetComponent<RectTransform>();
            var graphic = panelObject?.GetComponent<Graphic>();
            return new UnityPlaygroundUiPanelProof
            {
                name = "MinimapPanel",
                found = panel != null,
                active = panelObject != null && panelObject.activeInHierarchy,
                width = panel != null ? panel.rect.width : 0f,
                height = panel != null ? panel.rect.height : 0f,
                anchoredX = panel != null ? panel.anchoredPosition.x : 0f,
                anchoredY = panel != null ? panel.anchoredPosition.y : 0f,
                raycastTarget = graphic != null ? graphic.raycastTarget.ToString() : "missing"
            };
        }

        static UnityPlaygroundScreenToMapProof CollectScreenToMapProof(Camera mainCamera, WorldTilemap tilemap)
        {
            var screenX = Screen.width * 0.5f;
            var screenY = Screen.height * 0.5f;
            if (mainCamera == null || tilemap == null)
            {
                return new UnityPlaygroundScreenToMapProof
                {
                    status = "Failed",
                    evidence = "MainCamera or WorldTilemap was missing.",
                    screenX = screenX,
                    screenY = screenY,
                    tileX = -1,
                    tileY = -1
                };
            }

            var tile = tilemap.GetTileAtScreenPosition(mainCamera, new Vector3(screenX, screenY, 0f));
            return new UnityPlaygroundScreenToMapProof
            {
                status = tile != null ? "Passed" : "Failed",
                evidence = tile != null
                    ? $"Screen center resolves to map tile {tile.X},{tile.Y}."
                    : "Screen center did not resolve to an in-bounds map tile.",
                screenX = screenX,
                screenY = screenY,
                tileX = tile != null ? tile.X : -1,
                tileY = tile != null ? tile.Y : -1
            };
        }

        static UnityPlaygroundMinimapProof CollectMinimapProof(Scene scene, int mapWidth, int mapHeight)
        {
            var minimapObject = FindSceneObject(scene, "Minimap");
            var minimapRect = minimapObject?.GetComponent<RectTransform>();
            if (minimapRect == null)
            {
                return new UnityPlaygroundMinimapProof
                {
                    status = "Failed",
                    evidence = "Minimap RectTransform was missing."
                };
            }

            var corners = new Vector3[4];
            minimapRect.GetWorldCorners(corners);
            var center = (corners[0] + corners[2]) * 0.5f;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, center);
            var projected = MinimapInteraction.TryProjectMinimapScreenPointToMapTarget(
                minimapRect,
                screenPoint,
                null,
                mapWidth,
                mapHeight,
                out var target);
            var expectedX = mapWidth * 0.5f;
            var expectedY = mapHeight * 0.5f;
            var targetMatches = projected &&
                                Mathf.Abs(target.x - expectedX) <= 0.51f &&
                                Mathf.Abs(target.y - expectedY) <= 0.51f;
            return new UnityPlaygroundMinimapProof
            {
                status = targetMatches ? "Passed" : "Failed",
                evidence = targetMatches
                    ? $"Minimap center projects to map target {target.x:0.##},{target.y:0.##}."
                    : "Minimap center did not project to the expected map target.",
                screenX = screenPoint.x,
                screenY = screenPoint.y,
                targetX = projected ? target.x : -1f,
                targetY = projected ? target.y : -1f,
                expectedX = expectedX,
                expectedY = expectedY
            };
        }

        static UnityPlaygroundViewportTarget[] SupportedViewportTargets()
        {
            return new[]
            {
                new UnityPlaygroundViewportTarget
                {
                    label = "baseline-window",
                    width = 1280,
                    height = 720,
                    purpose = "Primary windowed desktop baseline and CanvasScaler reference resolution."
                },
                new UnityPlaygroundViewportTarget
                {
                    label = "common-window",
                    width = 1366,
                    height = 768,
                    purpose = "Common compact laptop/windowed target."
                },
                new UnityPlaygroundViewportTarget
                {
                    label = "full-hd",
                    width = 1920,
                    height = 1080,
                    purpose = "Primary full-size desktop target."
                }
            };
        }

        static string[] FixedSizeAssumptions()
        {
            return new[]
            {
                "CameraFollow derives orthographic size from Screen.height and its scene scale field.",
                "CursorManager scales overlay cursors against a 720px reference height and clamps the result.",
                "WorldTilemap screen picking converts Input.mousePosition through the main camera ScreenToWorldPoint path.",
                "MinimapInteraction maps the Minimap RectTransform local point into World.Current.Map dimensions.",
                "MinimapPanel and Minimap RectTransform geometry are the click target for the minimap path."
            };
        }

        static int EnsureRunning(UnityManager unityManager)
        {
            var ticks = 0;
            while (unityManager.ExecutionMode != ExecutionMode.Running && ticks < 4)
            {
                unityManager.FixedUpdate();
                ticks++;
            }

            if (unityManager.ExecutionMode != ExecutionMode.Running)
            {
                throw new InvalidOperationException($"UnityManager did not reach Running mode. Current mode: {unityManager.ExecutionMode}");
            }

            return ticks;
        }

        static void AdvanceTicks(UnityManager unityManager, int ticks)
        {
            for (var index = 0; index < ticks; index++)
            {
                unityManager.FixedUpdate();
            }
        }

        static int CountCommandsAfterId(UnityManager unityManager, int lastSeenCommandId)
        {
            return GetCommandsAfterId(unityManager, lastSeenCommandId).Count();
        }

        static bool HasQueuedCommand(UnityManager unityManager)
        {
            return unityManager.GameManager.ControllerProvider.CommandController.CommandExists(unityManager.LastCommandId + 1);
        }

        static Command GetNextCommand(UnityManager unityManager)
        {
            return GetCommandsAfterId(unityManager, unityManager.LastCommandId)
                .OrderBy(command => command.Id)
                .FirstOrDefault();
        }

        static IEnumerable<Command> GetCommandsAfterId(UnityManager unityManager, int lastSeenCommandId)
        {
            return unityManager.GameManager.ControllerProvider.CommandController.GetCommandsAfterId(lastSeenCommandId);
        }

        static UnityPlaygroundCommandTraceEntry CommandTraceEntry(Command command, int lastAdvancedCommandId)
        {
            return new UnityPlaygroundCommandTraceEntry
            {
                id = command.Id,
                commandType = command.GetType().Name,
                result = command.Result.ToString(),
                advanced = command.Id <= lastAdvancedCommandId,
                playerClan = command.Player != null && command.Player.Clan != null
                    ? command.Player.Clan.ShortName
                    : string.Empty
            };
        }

        static string CurrentClanName()
        {
            return Game.IsInitialized() && Game.Current.GetCurrentPlayer() != null
                ? Game.Current.GetCurrentPlayer().Clan.ShortName
                : string.Empty;
        }

        static UnityNewGameEntity CreateSettings(
            UnityPlaygroundOptions options,
            string world,
            int seed,
            Wism.Client.Data.Entities.ModKitSelectionEntity modKitSelection)
        {
            return new UnityNewGameEntity
            {
                InteractiveUI = false,
                IsNewGame = true,
                RandomSeed = ResolveSeed(options, seed),
                RandomStartLocations = false,
                WorldName = world,
                ModKitSelection = modKitSelection,
                Players = CreatePlayers(options)
            };
        }

        static UnityPlayerEntity[] CreatePlayers(UnityPlaygroundOptions options)
        {
            var total = Mathf.Clamp(options.HumanAgents + options.AiAgents, 2, ClassicClanOrder.Length);
            var humanCount = Mathf.Clamp(options.HumanAgents, 0, total);
            return ClassicClanOrder
                .Take(total)
                .Select((clan, index) => new UnityPlayerEntity
                {
                    ClanName = clan,
                    IsHuman = index < humanCount
                })
                .ToArray();
        }

        static int ResolveSeed(UnityPlaygroundOptions options, int selectionSeed = 0)
        {
            if (options.Seed > 0)
            {
                return options.Seed;
            }

            if (selectionSeed > 0)
            {
                return selectionSeed;
            }

            if (options.Fuzz)
            {
                return Math.Abs(StableHash(options.RunId));
            }

            return GameManager.DefaultRandom;
        }

        static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                foreach (var ch in value ?? string.Empty)
                {
                    hash = (hash * 31) + ch;
                }

                return hash == int.MinValue ? int.MaxValue : hash;
            }
        }

        static int CountCommand(IEnumerable<Command> commands, string nameFragment)
        {
            return commands.Count(command => command.GetType().Name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static IReadOnlyList<UnityPlaygroundInvariantEntry> CollectInvariants()
        {
            var entries = new List<UnityPlaygroundInvariantEntry>();
            if (!Game.IsInitialized() || TryGetCurrentWorld() == null)
            {
                entries.Add(new UnityPlaygroundInvariantEntry
                {
                    name = "game-initialized",
                    status = "Failed",
                    evidence = "Game or world was not initialized."
                });
                return entries;
            }

            var mixedTiles = new List<string>();
            var visitingTiles = new List<string>();
            var referencedArmyIds = new HashSet<int>();
            var map = World.Current.Map;
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var tile = map[x, y];
                    var armies = (tile.Armies ?? new List<Wism.Client.MapObjects.Army>())
                        .Concat(tile.VisitingArmies ?? new List<Wism.Client.MapObjects.Army>())
                        .Where(army => army != null && !army.IsDead)
                        .ToArray();
                    foreach (var army in armies)
                    {
                        referencedArmyIds.Add(army.Id);
                    }

                    var owners = armies
                        .Where(army => army.Player != null && army.Player.Clan != null)
                        .Select(army => army.Player.Clan.ShortName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    if (owners.Length > 1)
                    {
                        mixedTiles.Add($"{x},{y}:{string.Join("/", owners)}");
                    }

                    if (tile.VisitingArmies != null && tile.VisitingArmies.Any(army => army != null && !army.IsDead))
                    {
                        visitingTiles.Add($"{x},{y}");
                    }
                }
            }

            entries.Add(new UnityPlaygroundInvariantEntry
            {
                name = "mixed-hostile-tile-stacks",
                status = mixedTiles.Count == 0 ? "Passed" : "Failed",
                evidence = mixedTiles.Count == 0 ? "No live mixed-clan tile stacks found." : string.Join("; ", mixedTiles.Take(8))
            });
            entries.Add(new UnityPlaygroundInvariantEntry
            {
                name = "stale-visiting-armies",
                status = visitingTiles.Count == 0 ? "Passed" : "Failed",
                evidence = visitingTiles.Count == 0 ? "No visiting armies remained after command draining." : string.Join("; ", visitingTiles.Take(8))
            });

            var ghostArmies = Game.Current.Players
                .SelectMany(player => player.GetArmies())
                .Where(army => !army.IsDead && !referencedArmyIds.Contains(army.Id))
                .Select(army => $"{army.Id}:{army.Clan.ShortName}@{army.Tile?.X},{army.Tile?.Y}")
                .Take(8)
                .ToArray();
            entries.Add(new UnityPlaygroundInvariantEntry
            {
                name = "live-army-tile-references",
                status = ghostArmies.Length == 0 ? "Passed" : "Failed",
                evidence = ghostArmies.Length == 0 ? "All live armies are referenced by a tile." : string.Join("; ", ghostArmies)
            });

            var selectedArmies = Game.Current.ArmiesSelected()
                ? Game.Current.GetSelectedArmies()
                : null;
            var staleSelected = selectedArmies != null &&
                                selectedArmies.Any(army => army == null ||
                                                           army.IsDead ||
                                                           army.Player != Game.Current.GetCurrentPlayer());
            entries.Add(new UnityPlaygroundInvariantEntry
            {
                name = "selected-armies-current-player",
                status = staleSelected ? "Failed" : "Passed",
                evidence = staleSelected ? "Selected armies include dead/null/non-current-player armies." : "Selected armies are empty or owned by the current player."
            });

            var activeRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>()
                .Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
            entries.Add(new UnityPlaygroundInvariantEntry
            {
                name = "active-renderers",
                status = activeRenderers > 0 ? "Passed" : "Failed",
                evidence = $"{activeRenderers} active renderer(s) found."
            });

            return entries;
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
            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);
            var camera = UnityEngine.Object.FindObjectsOfType<Camera>()
                .Where(item => item != null && item.enabled && item.gameObject.activeInHierarchy)
                .OrderBy(item => item.depth)
                .FirstOrDefault();
            if (camera == null)
            {
                ScreenCapture.CaptureScreenshot(path);
                return path;
            }

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var renderTexture = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }

            return path;
        }

        static string SafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string((value ?? string.Empty)
                .Select(ch => invalid.Contains(ch) ? '-' : ch)
                .ToArray());
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

        static GameObject FindSceneObject(Scene scene, string name)
        {
            return scene.IsValid()
                ? scene.GetRootGameObjects()
                    .SelectMany(Flatten)
                    .FirstOrDefault(go => string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase))
                : null;
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
            public string Command = "world";
            public string World = DefaultWorld;
            public string Profile = string.Empty;
            public string[] Packs = new string[0];
            public string ModRoot = string.Empty;
            public string ScenePath = DefaultScene;
            public string Scenario = "smoke";
            public string RunId = $"unity-smoke-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            public string OutputDirectory = Path.Combine("artifacts", "unity-playground", "smoke");
            public bool CaptureScreenshot;
            public bool AdvanceBootstrap;
            public int MaxTicks = 64;
            public int Turns = 8;
            public int HumanAgents = 1;
            public int AiAgents = 1;
            public int Seed = 0;
            public bool Fuzz;
            public int MaxCommandStalls = 12;
            public string HumanDecisionScriptPath = string.Empty;
            public int ScreenWidth = 0;
            public int ScreenHeight = 0;
            public string ScreenLabel = string.Empty;
            public bool RequireActualResolution;

            public static UnityPlaygroundOptions FromCommandLine(string[] args)
            {
                var options = new UnityPlaygroundOptions();
                var values = ParseArgs(args);
                options.Command = Read(values, "command", options.Command);
                options.Profile = Read(values, "profile", options.Profile);
                options.Packs = ReadCsv(values, "packs");
                options.ModRoot = Read(values, "modRoot", options.ModRoot);
                options.World = Read(values, "world", options.World);
                options.ScenePath = Read(values, "scene", options.ScenePath);
                options.Scenario = Read(values, "scenario", options.Scenario);
                options.RunId = Read(values, "runId", options.RunId);
                options.CaptureScreenshot = ReadBool(values, "screenshot", false);
                options.AdvanceBootstrap = ReadBool(values, "advanceBootstrap", false);
                options.MaxTicks = ReadInt(values, "maxTicks", options.MaxTicks);
                options.Turns = ReadInt(values, "turns", options.Turns);
                options.HumanAgents = ReadInt(values, "humanAgents", options.HumanAgents);
                options.AiAgents = ReadInt(values, "aiAgents", options.AiAgents);
                if (IsMixedMode(options.Scenario) && !values.ContainsKey("humanAgents") && !values.ContainsKey("aiAgents"))
                {
                    options.HumanAgents = 2;
                    options.AiAgents = 6;
                }

                options.Seed = ReadInt(values, "seed", options.Seed);
                options.Fuzz = ReadBool(values, "fuzz", false);
                options.MaxCommandStalls = ReadInt(values, "maxCommandStalls", options.MaxCommandStalls);
                options.HumanDecisionScriptPath = Read(values, "humanDecisionScript", Read(values, "human-decision-script", options.HumanDecisionScriptPath));
                options.ScreenWidth = ReadInt(values, "screenWidth", ReadInt(values, "screen-width", options.ScreenWidth));
                options.ScreenHeight = ReadInt(values, "screenHeight", ReadInt(values, "screen-height", options.ScreenHeight));
                options.ScreenLabel = Read(values, "screenLabel", Read(values, "screen-label", options.ScreenLabel));
                options.RequireActualResolution = ReadBool(values, "requireActualResolution", ReadBool(values, "require-actual-resolution", false));
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

            static int ReadInt(Dictionary<string, string> values, string name, int fallback)
            {
                return values.TryGetValue(name, out var value) && int.TryParse(value, out var parsed)
                    ? Math.Max(1, parsed)
                    : fallback;
            }

            static bool ReadBool(Dictionary<string, string> values, string name, bool fallback)
            {
                return values.TryGetValue(name, out var value) && bool.TryParse(value, out var parsed)
                    ? parsed
                    : fallback;
            }

            static string[] ReadCsv(Dictionary<string, string> values, string name)
            {
                if (!values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    return new string[0];
                }

                return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToArray();
            }

            static bool IsMixedMode(string scenario)
            {
                return string.Equals(scenario, "mixed-human-ai-marathon", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(scenario, "mixed-mode", StringComparison.OrdinalIgnoreCase);
            }
        }

        static readonly string[] ClassicClanOrder =
        {
            "Sirians",
            "StormGiants",
            "Elvallie",
            "OrcsOfKor",
            "Selentines",
            "HorseLords",
            "GreyDwarves",
            "LordBane"
        };

        [Serializable]
        sealed class UnityPlaygroundHumanDecisionScript
        {
            public int schemaVersion = 1;
            public UnityPlaygroundHumanDecision[] decisions = new UnityPlaygroundHumanDecision[0];

            public bool available;
            int cursor;

            public static UnityPlaygroundHumanDecisionScript Load(string path)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return new UnityPlaygroundHumanDecisionScript();
                }

                var script = JsonUtility.FromJson<UnityPlaygroundHumanDecisionScript>(File.ReadAllText(path));
                if (script == null)
                {
                    return new UnityPlaygroundHumanDecisionScript();
                }

                script.available = true;
                script.decisions = script.decisions ?? new UnityPlaygroundHumanDecision[0];
                return script;
            }

            public UnityPlaygroundHumanDecision Next(string clan)
            {
                for (var index = cursor; index < decisions.Length; index++)
                {
                    var decision = decisions[index];
                    if (decision == null)
                    {
                        cursor = index + 1;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(decision.clan) ||
                        string.Equals(decision.clan, clan, StringComparison.OrdinalIgnoreCase))
                    {
                        cursor = index + 1;
                        return decision;
                    }
                }

                return null;
            }
        }

        [Serializable]
        sealed class UnityPlaygroundHumanDecision
        {
            public string clan;
            public string action;
            public int x = -1;
            public int y = -1;
            public string note;
        }
    }
}
