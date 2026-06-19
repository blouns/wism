using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Companion.Shared.Events;

namespace WismCompanion.State
{
    /// <summary>
    /// Central, main-thread-only store for the companion. Holds the latest map snapshot per channel
    /// plus a bounded per-channel event log, tracks the known channels and the active channel, and
    /// raises change events the UI subscribes to. All mutators are expected to be called from the
    /// Unity main thread (the bootstrap drains the transport queue and forwards here).
    /// </summary>
    public sealed class CompanionState
    {
        private readonly Dictionary<string, MapSnapshot> latestByChannel =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> channels = new();
        private readonly Dictionary<string, DateTime> lastActiveByChannel =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly LogBuffer log = new();
        private readonly ReplayBuffer replay = new();

        /// <summary>Raised when the active channel's map or log changed (UI should refresh).</summary>
        public event Action Changed;

        /// <summary>Raised when the set of known channels changed (UI should rebuild the selector).</summary>
        public event Action ChannelsChanged;

        public IReadOnlyList<string> Channels => channels;

        public string SelectedChannel { get; private set; } = TelemetryContext.DefaultChannelId;

        public long TotalEventsReceived { get; private set; }

        public ReplayViewMode ReplayMode => replay.Mode;

        public void SelectChannel(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId) || string.Equals(channelId, SelectedChannel, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SelectedChannel = channelId;
            replay.SelectChannel(channelId);
            Changed?.Invoke();
        }

        public void ApplyMap(MapSnapshot map)
        {
            if (map == null)
            {
                return;
            }

            var channel = TelemetryContext.ChannelIdOrDefault(map.Telemetry);
            latestByChannel[channel] = map;
            replay.RecordMap(map);
            log.Add(CompanionLogEntry.FromMap(map));
            TotalEventsReceived++;
            TrackChannel(channel);
            RaiseIfActive(channel);
        }

        public void ApplyCommand(CommandExecutedEvent command)
        {
            if (command == null)
            {
                return;
            }

            var channel = TelemetryContext.ChannelIdOrDefault(command.Telemetry);
            replay.RecordCommand(command);
            log.Add(CompanionLogEntry.FromCommand(command));
            TotalEventsReceived++;
            TrackChannel(channel);
            RaiseIfActive(channel);
        }

        public MapSnapshot GetLatestMap(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return null;
            }

            return latestByChannel.TryGetValue(channelId, out var map) ? map : null;
        }

        public MapSnapshot GetVisibleMap(string channelId)
        {
            return replay.GetVisibleMap(channelId, GetLatestMap(channelId));
        }

        public IReadOnlyList<CompanionLogEntry> GetLog(string channelId) => log.GetEntries(channelId);

        public string ReplayStatusText => replay.GetStatusText(SelectedChannel);

        public IReadOnlyList<ReplayTurn> GetReplayTurns(string channelId) => replay.GetTurns(channelId);

        public bool EnterReplay()
        {
            var changed = replay.EnterReplay(SelectedChannel);
            if (changed)
            {
                Changed?.Invoke();
            }

            return changed;
        }

        public void GoLive()
        {
            replay.GoLive(SelectedChannel);
            Changed?.Invoke();
        }

        public void PreviousReplayFrame()
        {
            if (replay.PreviousFrame(SelectedChannel))
            {
                Changed?.Invoke();
            }
        }

        public void NextReplayFrame()
        {
            if (replay.NextFrame(SelectedChannel))
            {
                Changed?.Invoke();
            }
        }

        public void PreviousReplayTurn()
        {
            if (replay.PreviousTurn(SelectedChannel))
            {
                Changed?.Invoke();
            }
        }

        public void NextReplayTurn()
        {
            if (replay.NextTurn(SelectedChannel))
            {
                Changed?.Invoke();
            }
        }

        public string ExportReplayJson(string channelId = null) =>
            replay.ExportJson(string.IsNullOrWhiteSpace(channelId) ? SelectedChannel : channelId);

        public string SaveReplayJson(string directory, string channelId = null) =>
            replay.SaveJson(string.IsNullOrWhiteSpace(channelId) ? SelectedChannel : channelId, directory);

        /// <summary>
        /// Removes channels that have not received any message within <paramref name="timeout"/>.
        /// Call periodically from the main thread (e.g., every 15 seconds).
        /// </summary>
        public void PruneStaleChannels(TimeSpan timeout)
        {
            var cutoff = DateTime.UtcNow - timeout;
            var stale = lastActiveByChannel
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            if (stale.Count == 0) return;

            foreach (var ch in stale)
            {
                channels.RemoveAll(c => string.Equals(c, ch, StringComparison.OrdinalIgnoreCase));
                latestByChannel.Remove(ch);
                lastActiveByChannel.Remove(ch);
            }

            if (!channels.Contains(SelectedChannel, StringComparer.OrdinalIgnoreCase))
                SelectedChannel = channels.Count > 0 ? channels[0] : TelemetryContext.DefaultChannelId;

            ChannelsChanged?.Invoke();
            Changed?.Invoke();
        }

        private void TrackChannel(string channel)
        {
            lastActiveByChannel[channel] = DateTime.UtcNow;

            if (channels.Contains(channel, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            channels.Add(channel);
            if (channels.Count == 1)
            {
                SelectedChannel = channel;
            }

            ChannelsChanged?.Invoke();
        }

        private void RaiseIfActive(string channel)
        {
            if (string.Equals(channel, SelectedChannel, StringComparison.OrdinalIgnoreCase))
            {
                Changed?.Invoke();
            }
        }
    }
}
