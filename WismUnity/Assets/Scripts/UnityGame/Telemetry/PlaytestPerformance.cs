using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Assets.Scripts.Managers;
using Assets.Scripts.Persistance.Entities;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using Wism.Client.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Diagnostics;

namespace Assets.Scripts.Telemetry
{
    // Opt-in only. No scene changes, network transport, or dependency on an Editor.
    public sealed class PlaytestPerformance : MonoBehaviour
    {
        public static PlaytestPerformance Instance { get; private set; }
        private LocalPerformanceRecorder recorder;
        private PerformanceProbe probe;
        private string output;
        private long decision, lastFrame, decisionAllocated;
        private int checkpoints, lastCheckpointTurn = -1, captures;
        private bool bookmarkPending, captureArmed, capturing;
        private double captureStarted;
        private readonly Dictionary<int, long> commands = new Dictionary<int, long>();
        private ProfilerRecorder mainThread, renderThread, gcAlloc;
        private int gc0, gc1, gc2;
        public string OutputDirectory => output;
        public bool Recording => recorder?.IsRecording == true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Environment.GetCommandLineArgs().Contains("-wism-profile") && Instance == null)
                new GameObject("Playtest diagnostics").AddComponent<PlaytestPerformance>();
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            try
            {
                var args = Environment.GetCommandLineArgs();
                var option = Array.IndexOf(args, "-wism-profile-output");
                var root = option >= 0 && option + 1 < args.Length ? args[option + 1] :
                    Path.Combine(Application.persistentDataPath, "Diagnostics");
                output = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                recorder = new LocalPerformanceRecorder(output);
                probe = new PerformanceProbe(recorder.Record, name => Profiler.BeginSample(name), () => Profiler.EndSample());
                mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
                renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 1);
                gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
                lastFrame = Stopwatch.GetTimestamp();
                gc0 = GC.CollectionCount(0); gc1 = GC.CollectionCount(1); gc2 = GC.CollectionCount(2);
                InputSystem.onEvent += OnInputEvent;
                File.WriteAllText(Path.Combine(output, "session.json"), JsonConvert.SerializeObject(new {
                    Schema = 1, StartedUtc = DateTime.UtcNow, ExpiresUtc = DateTime.UtcNow.AddDays(7),
                    Application.buildGUID, Application.version, Unity = Application.unityVersion,
                    StopwatchFrequency = Stopwatch.Frequency, InputClock = "InputSystem event time; separate from Stopwatch ticks",
                    SystemInfo.processorType, SystemInfo.processorCount, SystemInfo.systemMemorySize,
                    SystemInfo.graphicsDeviceName, Development = UnityEngine.Debug.isDebugBuild,
                    AiProfile = "tactical", PathCache = "absent", RepeatedPathRequests = "unavailable",
                    GpuTime = "unavailable", InputToPresent = "unavailable", MaxHours = 2,
                    TimingRule = "scope inclusive and exclusive; timing-callback rows are aliases, never additive",
                    RawCaptureLimit = "3 one-frame captures; 512 MiB checked at frame boundaries, not a hard in-frame limit"
                }, Formatting.Indented));
                UnityEngine.Debug.Log("Playtest diagnostics: " + output);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("Diagnostics unavailable: " + ex.Message); enabled = false; }
        }

        private void OnInputEvent(InputEventPtr evt, InputDevice device)
        {
            if (!(device is Keyboard keyboard) || (!evt.IsA<StateEvent>() && !evt.IsA<DeltaStateEvent>())) return;
            if (keyboard.f9Key.ReadValueFromEvent(evt, out var bookmark) && bookmark > .5f && !keyboard.f9Key.isPressed)
            {
                recorder?.Record(new PerformanceSample { Kind = "bookmark", Name = "slowdown",
                    Decision = decision, InputEventSeconds = evt.time, Detail = "Timestamp=processing Stopwatch ticks" });
                bookmarkPending = true;
            }
            if (keyboard.f10Key.ReadValueFromEvent(evt, out var capture) && capture > .5f && !keyboard.f10Key.isPressed)
            {
                captureArmed = true;
                recorder?.Record(new PerformanceSample { Kind = "capture-armed", InputEventSeconds = evt.time });
            }
        }

        private void Update()
        {
            if (!Recording) { StopCapture(); return; }
            var now = Stopwatch.GetTimestamp();
            recorder.Record(new PerformanceSample { Kind = "frame", Timestamp = now, Decision = decision,
                DurationMs = (now - lastFrame) * 1000d / Stopwatch.Frequency,
                Bytes = gcAlloc.Valid && gcAlloc.Count > 0 ? gcAlloc.LastValue : (long?)null,
                MainThreadMs = mainThread.Valid && mainThread.Count > 0 ? mainThread.LastValue / 1000000d : (double?)null,
                RenderThreadMs = renderThread.Valid && renderThread.Count > 0 ? renderThread.LastValue / 1000000d : (double?)null,
                Detail = "MainThreadMs, RenderThreadMs and Bytes (allocation) refer to the prior completed frame" });
            lastFrame = now;
            var a = GC.CollectionCount(0); var b = GC.CollectionCount(1); var c = GC.CollectionCount(2);
            if (a != gc0 || b != gc1 || c != gc2)
                recorder.Record(new PerformanceSample { Kind = "gc", Gc0 = a - gc0, Gc1 = b - gc1, Gc2 = c - gc2 });
            gc0 = a; gc1 = b; gc2 = c;
            if (capturing && Time.realtimeSinceStartupAsDouble > captureStarted) StopCapture();
        }

        public void BeginDecision(UnityManager manager)
        {
            if (!Recording) return;
            var player = Game.Current.GetCurrentPlayer();
            var turn = player.Turn;
            if (checkpoints < 6 && (bookmarkPending || checkpoints == 0 || (turn >= lastCheckpointTurn + 10)))
            {
                SaveCheckpoint(manager);
                lastCheckpointTurn = turn;
                bookmarkPending = false;
            }
            if (captureArmed && !capturing && captures < 3 && UnityEngine.Debug.isDebugBuild)
            {
                captureArmed = false;
                var rawBytes = Directory.GetFiles(output, "*.raw").Sum(path => new FileInfo(path).Length);
                if (rawBytes < 384L * 1024 * 1024 && new DriveInfo(Path.GetPathRoot(output)).AvailableFreeSpace > 81L * 1024 * 1024 * 1024)
                {
                    captures++;
                    Profiler.maxUsedMemory = 16 * 1024 * 1024;
                    Profiler.logFile = Path.Combine(output, "capture-" + captures + ".raw");
                    Profiler.enableBinaryLog = true;
                    Profiler.enabled = true;
                    capturing = true;
                    captureStarted = Time.realtimeSinceStartupAsDouble;
                }
            }
            decision++;
            probe.DecisionId = decision;
            probe.ResetPaths();
            PerformanceProbe.Current = probe;
            var armies = Game.Current.Players.SelectMany(p => p.GetArmies()).ToList();
            recorder.Record(new PerformanceSample { Kind = "decision-start", Decision = decision,
                Detail = JsonConvert.SerializeObject(new {
                    turn, Clan = player.Clan.ShortName, Difficulty = player.AiDifficulty.ToString(),
                    World = World.Current.Name, Game.Current.RandomSeed, Armies = armies.Count,
                    Stacks = armies.GroupBy(a => new { a.X, a.Y, a.Player }).Count(),
                    OwnedCities = World.Current.GetCities().Count(c => c.Player != null && c.Clan.ShortName != "Neutral"),
                    NeutralCities = World.Current.GetCities().Count(c => c.Player == null || c.Clan.ShortName == "Neutral"),
                    ActiveClans = Game.Current.Players.Count(p => !p.IsDead),
                    manager.ObserveAiMovement, manager.ShowAiCombat }) });
            decisionAllocated = GC.GetAllocatedBytesForCurrentThread();
        }

        public void Timing(string name, TimeSpan elapsed)
        {
            if (Recording) recorder.Record(new PerformanceSample { Kind = "timing-alias", Name = name,
                Decision = decision, DurationMs = elapsed.TotalMilliseconds });
        }

        public void EndDecision(IEnumerable<ICommandAction> queued)
        {
            if (PerformanceProbe.Current == probe) PerformanceProbe.Current = null;
            if (!Recording) return;
            int count = 0;
            foreach (var action in queued)
            {
                if (!(action is Command command)) continue;
                if (commands.Count < 8192) commands[command.Id] = decision;
                recorder.Record(new PerformanceSample { Kind = "queued", Name = command.GetType().Name,
                    Command = command.Id, Decision = decision });
                count++;
            }
            recorder.Record(new PerformanceSample { Kind = "decision-end", Decision = decision, Count = count,
                Bytes = GC.GetAllocatedBytesForCurrentThread() - decisionAllocated });
            recorder.Record(new PerformanceSample { Kind = "paths", Decision = decision, PathQueries = probe.PathQueries,
                ExpandedNodes = probe.ExpandedNodes, GraphCells = probe.GraphCells, Detail = "cache=absent; repeated-query metric unavailable" });
        }

        public void CommandResult(Command command, ActionState state, double elapsed, bool rejected = false)
        {
            if (!Recording) return;
            commands.TryGetValue(command.Id, out var origin);
            recorder.Record(new PerformanceSample { Kind = rejected ? "command-rejected" : "command-attempt",
                Name = command.GetType().Name, Command = command.Id, Decision = origin,
                DurationMs = elapsed, Detail = state.ToString() });
            if (state == ActionState.Succeeded || state == ActionState.Failed || rejected) commands.Remove(command.Id);
        }

        public void Measure(string name, Action action)
        {
            if (!Recording) { action(); return; }
            var previous = PerformanceProbe.Current;
            PerformanceProbe.Current = probe;
            try { using (PerformanceProbe.Measure(name)) action(); }
            finally { PerformanceProbe.Current = previous; }
        }

        private void SaveCheckpoint(UnityManager manager)
        {
            try
            {
                Measure("diagnostic-checkpoint", () => {
                    var snapshot = new UnityGameEntity("Diagnostic checkpoint", manager) { WismGameEntity = Game.Current.Snapshot() };
                    var json = JsonConvert.SerializeObject(snapshot);
                    if (System.Text.Encoding.UTF8.GetByteCount(json) > 8 * 1024 * 1024) return;
                    var path = Path.Combine(output, "checkpoint-" + (++checkpoints) + ".json");
                    File.WriteAllText(path, json);
                    using (var sha = SHA256.Create())
                    using (var stream = File.OpenRead(path))
                        recorder.Record(new PerformanceSample { Kind = "checkpoint", Name = Path.GetFileName(path),
                            Detail = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""), Decision = decision + 1 });
                });
            }
            catch (Exception ex) { recorder.Record(new PerformanceSample { Kind = "checkpoint-error", Detail = ex.GetType().Name }); }
        }

        private void StopCapture()
        {
            if (!capturing) return;
            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = "";
            capturing = false;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            InputSystem.onEvent -= OnInputEvent;
            StopCapture();
            if (PerformanceProbe.Current == probe) PerformanceProbe.Current = null;
            recorder?.Dispose();
            mainThread.Dispose(); renderThread.Dispose(); gcAlloc.Dispose();
            Instance = null;
        }
    }
}
