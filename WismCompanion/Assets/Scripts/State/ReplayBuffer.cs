using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Wism.Companion.Shared.Events;

namespace WismCompanion.State
{
    public enum ReplayViewMode
    {
        Live,
        Replay
    }

    public sealed class ReplayFrame
    {
        public ReplayFrame(
            MapSnapshot snapshot,
            DateTime timestampUtc,
            string channelId,
            string turnKey,
            int frameIndexInTurn,
            bool isTurnBoundary,
            CommandExecutedEvent triggerCommand)
        {
            Snapshot = snapshot;
            TimestampUtc = timestampUtc;
            ChannelId = channelId;
            TurnKey = turnKey;
            FrameIndexInTurn = frameIndexInTurn;
            IsTurnBoundary = isTurnBoundary;
            TriggerCommand = triggerCommand;
        }

        public MapSnapshot Snapshot { get; }
        public DateTime TimestampUtc { get; }
        public string ChannelId { get; }
        public string TurnKey { get; }
        public int FrameIndexInTurn { get; }
        public bool IsTurnBoundary { get; }
        public CommandExecutedEvent TriggerCommand { get; }
    }

    public sealed class ReplayTurn
    {
        private readonly List<ReplayFrame> frames = new();

        public ReplayTurn(string turnKey, DateTime startedUtc)
        {
            TurnKey = turnKey;
            StartedUtc = startedUtc;
        }

        public string TurnKey { get; }
        public DateTime StartedUtc { get; }
        public DateTime? EndedUtc { get; private set; }
        public IReadOnlyList<ReplayFrame> Frames => frames;

        public bool IsComplete => EndedUtc.HasValue;

        public ReplayFrame AddFrame(
            MapSnapshot snapshot,
            string channelId,
            bool isTurnBoundary,
            CommandExecutedEvent triggerCommand)
        {
            var frame = new ReplayFrame(
                snapshot,
                snapshot.Timestamp == default ? DateTime.UtcNow : snapshot.Timestamp,
                channelId,
                TurnKey,
                frames.Count,
                isTurnBoundary,
                triggerCommand);
            frames.Add(frame);
            return frame;
        }

        public void Complete(DateTime endedUtc)
        {
            EndedUtc = endedUtc;
        }
    }

    public sealed class ReplayBuffer
    {
        public const int DefaultCompletedTurnRetention = 2;

        private readonly Dictionary<string, ChannelReplay> channels = new(StringComparer.OrdinalIgnoreCase);

        public ReplayBuffer(int completedTurnRetention = DefaultCompletedTurnRetention)
        {
            CompletedTurnRetention = Math.Max(0, completedTurnRetention);
        }

        public int CompletedTurnRetention { get; }

        public ReplayViewMode Mode { get; private set; } = ReplayViewMode.Live;

        public void RecordCommand(CommandExecutedEvent command)
        {
            if (command == null)
            {
                return;
            }

            var channel = TelemetryContext.ChannelIdOrDefault(command.Telemetry);
            GetOrCreateChannel(channel).PendingCommand = command;
        }

        public ReplayFrame RecordMap(MapSnapshot map)
        {
            if (map == null)
            {
                return null;
            }

            var channel = TelemetryContext.ChannelIdOrDefault(map.Telemetry);
            var replay = GetOrCreateChannel(channel);
            var command = replay.PendingCommand;
            replay.PendingCommand = null;

            var isBoundary = command != null &&
                string.Equals(command.CommandType, "EndTurnCommand", StringComparison.OrdinalIgnoreCase);

            if (replay.Turns.Count == 0)
            {
                replay.Turns.Add(new ReplayTurn(NextTurnKey(1), map.Timestamp));
            }
            else if (isBoundary && replay.Turns[^1].Frames.Count > 0)
            {
                replay.Turns[^1].Complete(command.Timestamp == default ? map.Timestamp : command.Timestamp);
                replay.Turns.Add(new ReplayTurn(NextTurnKey(replay.NextTurnNumber++), map.Timestamp));
            }

            var turn = replay.Turns[^1];
            var frame = turn.AddFrame(map, channel, isBoundary, command);
            Trim(replay);
            if (Mode == ReplayViewMode.Live)
            {
                MoveCursorToLatest(replay);
            }

            return frame;
        }

        public void SelectChannel(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return;
            }

            if (channels.TryGetValue(channelId, out var replay) && Mode == ReplayViewMode.Live)
            {
                MoveCursorToLatest(replay);
            }
        }

        public bool EnterReplay(string channelId)
        {
            if (!TryGetNonEmptyChannel(channelId, out var replay))
            {
                return false;
            }

            MoveCursorToLatest(replay);
            Mode = ReplayViewMode.Replay;
            return true;
        }

        public void GoLive(string channelId)
        {
            Mode = ReplayViewMode.Live;
            if (channels.TryGetValue(channelId, out var replay))
            {
                MoveCursorToLatest(replay);
            }
        }

        public bool PreviousFrame(string channelId)
        {
            if (!TryGetNonEmptyChannel(channelId, out var replay))
            {
                return false;
            }

            Mode = ReplayViewMode.Replay;
            if (replay.CursorFrameIndex > 0)
            {
                replay.CursorFrameIndex--;
                return true;
            }

            if (replay.CursorTurnIndex <= 0)
            {
                return false;
            }

            replay.CursorTurnIndex--;
            replay.CursorFrameIndex = replay.Turns[replay.CursorTurnIndex].Frames.Count - 1;
            return true;
        }

        public bool NextFrame(string channelId)
        {
            if (!TryGetNonEmptyChannel(channelId, out var replay))
            {
                return false;
            }

            Mode = ReplayViewMode.Replay;
            var turn = replay.Turns[replay.CursorTurnIndex];
            if (replay.CursorFrameIndex < turn.Frames.Count - 1)
            {
                replay.CursorFrameIndex++;
                return true;
            }

            if (replay.CursorTurnIndex >= replay.Turns.Count - 1)
            {
                return false;
            }

            replay.CursorTurnIndex++;
            replay.CursorFrameIndex = 0;
            return true;
        }

        public bool PreviousTurn(string channelId)
        {
            if (!TryGetNonEmptyChannel(channelId, out var replay) || replay.CursorTurnIndex <= 0)
            {
                return false;
            }

            Mode = ReplayViewMode.Replay;
            replay.CursorTurnIndex--;
            replay.CursorFrameIndex = 0;
            return true;
        }

        public bool NextTurn(string channelId)
        {
            if (!TryGetNonEmptyChannel(channelId, out var replay) ||
                replay.CursorTurnIndex >= replay.Turns.Count - 1)
            {
                return false;
            }

            Mode = ReplayViewMode.Replay;
            replay.CursorTurnIndex++;
            replay.CursorFrameIndex = 0;
            return true;
        }

        public MapSnapshot GetVisibleMap(string channelId, MapSnapshot liveMap)
        {
            return Mode == ReplayViewMode.Replay && TryGetCursorFrame(channelId, out var frame)
                ? frame.Snapshot
                : liveMap;
        }

        public string GetStatusText(string channelId)
        {
            if (!TryGetNonEmptyChannel(channelId, out var replay))
            {
                return Mode == ReplayViewMode.Replay ? "Replay: no frames" : "Live: recording";
            }

            var turn = replay.Turns[replay.CursorTurnIndex];
            var mode = Mode == ReplayViewMode.Replay ? "Replay" : "Live";
            return $"{mode}: {turn.TurnKey} frame {replay.CursorFrameIndex + 1}/{turn.Frames.Count}";
        }

        public IReadOnlyList<ReplayTurn> GetTurns(string channelId)
        {
            return channels.TryGetValue(channelId, out var replay)
                ? replay.Turns
                : Array.Empty<ReplayTurn>();
        }

        public string ExportJson(string channelId)
        {
            var export = BuildExport(channelId);
            return JsonConvert.SerializeObject(export, Formatting.Indented);
        }

        public string SaveJson(string channelId, string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("A replay export directory is required.", nameof(directory));
            }

            Directory.CreateDirectory(directory);
            var safeChannel = string.Concat((channelId ?? TelemetryContext.DefaultChannelId)
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'));
            var path = Path.Combine(directory, $"wism-replay-{safeChannel}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(path, ExportJson(channelId));
            return path;
        }

        private ReplayExport BuildExport(string channelId)
        {
            channels.TryGetValue(channelId ?? string.Empty, out var replay);
            var turns = replay?.Turns ?? new List<ReplayTurn>();
            return new ReplayExport
            {
                FormatVersion = 1,
                Channel = string.IsNullOrWhiteSpace(channelId) ? TelemetryContext.DefaultChannelId : channelId,
                ExportedAtUtc = DateTime.UtcNow,
                CompletedTurnRetention = CompletedTurnRetention,
                Turns = turns.Select(turn => new ReplayTurnExport
                {
                    TurnKey = turn.TurnKey,
                    StartedUtc = turn.StartedUtc,
                    EndedUtc = turn.EndedUtc,
                    Frames = turn.Frames.Select(frame => new ReplayFrameExport
                    {
                        TimestampUtc = frame.TimestampUtc,
                        ChannelId = frame.ChannelId,
                        TurnKey = frame.TurnKey,
                        FrameIndexInTurn = frame.FrameIndexInTurn,
                        IsTurnBoundary = frame.IsTurnBoundary,
                        Snapshot = frame.Snapshot,
                        TriggerCommand = ToCommandExport(frame.TriggerCommand)
                    }).ToList()
                }).ToList()
            };
        }

        private static ReplayCommandExport ToCommandExport(CommandExecutedEvent command)
        {
            if (command == null)
            {
                return null;
            }

            return new ReplayCommandExport
            {
                CommandType = command.CommandType,
                ActorId = command.ActorId,
                TargetId = command.TargetId,
                TargetPosition = command.TargetPosition,
                Result = command.Result,
                TimestampUtc = command.Timestamp,
                Parameters = command.Parameters?
                    .Select(kvp => new ReplayParameterExport
                    {
                        Key = kvp.Key,
                        Value = kvp.Value?.ToString() ?? string.Empty
                    })
                    .ToList() ?? new List<ReplayParameterExport>()
            };
        }

        private ChannelReplay GetOrCreateChannel(string channel)
        {
            if (!channels.TryGetValue(channel, out var replay))
            {
                replay = new ChannelReplay();
                channels[channel] = replay;
            }

            return replay;
        }

        private bool TryGetNonEmptyChannel(string channelId, out ChannelReplay replay)
        {
            if (channels.TryGetValue(channelId ?? string.Empty, out replay) && replay.Turns.Count > 0)
            {
                ClampCursor(replay);
                return replay.Turns[replay.CursorTurnIndex].Frames.Count > 0;
            }

            return false;
        }

        private bool TryGetCursorFrame(string channelId, out ReplayFrame frame)
        {
            frame = null;
            if (!TryGetNonEmptyChannel(channelId, out var replay))
            {
                return false;
            }

            frame = replay.Turns[replay.CursorTurnIndex].Frames[replay.CursorFrameIndex];
            return true;
        }

        private void Trim(ChannelReplay replay)
        {
            while (replay.Turns.Count > CompletedTurnRetention + 1)
            {
                replay.Turns.RemoveAt(0);
                replay.CursorTurnIndex = Math.Max(0, replay.CursorTurnIndex - 1);
            }

            ClampCursor(replay);
        }

        private static void MoveCursorToLatest(ChannelReplay replay)
        {
            if (replay.Turns.Count == 0)
            {
                replay.CursorTurnIndex = 0;
                replay.CursorFrameIndex = 0;
                return;
            }

            replay.CursorTurnIndex = replay.Turns.Count - 1;
            replay.CursorFrameIndex = Math.Max(0, replay.Turns[^1].Frames.Count - 1);
        }

        private static void ClampCursor(ChannelReplay replay)
        {
            if (replay.Turns.Count == 0)
            {
                replay.CursorTurnIndex = 0;
                replay.CursorFrameIndex = 0;
                return;
            }

            replay.CursorTurnIndex = Math.Clamp(replay.CursorTurnIndex, 0, replay.Turns.Count - 1);
            var turn = replay.Turns[replay.CursorTurnIndex];
            replay.CursorFrameIndex = Math.Clamp(replay.CursorFrameIndex, 0, Math.Max(0, turn.Frames.Count - 1));
        }

        private static string NextTurnKey(int turnNumber) => $"turn-{turnNumber:0000}";

        private sealed class ChannelReplay
        {
            public List<ReplayTurn> Turns { get; } = new();
            public CommandExecutedEvent PendingCommand { get; set; }
            public int CursorTurnIndex { get; set; }
            public int CursorFrameIndex { get; set; }
            public int NextTurnNumber { get; set; } = 2;
        }

        private sealed class ReplayExport
        {
            public int FormatVersion { get; set; }
            public string Channel { get; set; }
            public DateTime ExportedAtUtc { get; set; }
            public int CompletedTurnRetention { get; set; }
            public List<ReplayTurnExport> Turns { get; set; } = new();
        }

        private sealed class ReplayTurnExport
        {
            public string TurnKey { get; set; }
            public DateTime StartedUtc { get; set; }
            public DateTime? EndedUtc { get; set; }
            public List<ReplayFrameExport> Frames { get; set; } = new();
        }

        private sealed class ReplayFrameExport
        {
            public DateTime TimestampUtc { get; set; }
            public string ChannelId { get; set; }
            public string TurnKey { get; set; }
            public int FrameIndexInTurn { get; set; }
            public bool IsTurnBoundary { get; set; }
            public MapSnapshot Snapshot { get; set; }
            public ReplayCommandExport TriggerCommand { get; set; }
        }

        private sealed class ReplayCommandExport
        {
            public string CommandType { get; set; }
            public string ActorId { get; set; }
            public string TargetId { get; set; }
            public object TargetPosition { get; set; }
            public string Result { get; set; }
            public DateTime TimestampUtc { get; set; }
            public List<ReplayParameterExport> Parameters { get; set; } = new();
        }

        private sealed class ReplayParameterExport
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}
