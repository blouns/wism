using System;
using System.Collections.Generic;

namespace WismCompanion.State
{
    /// <summary>
    /// Channel-aware, bounded log buffer (newest first). Ported from the WinForms companion's
    /// TelemetryLogBuffer: at most <see cref="MaxEntriesPerChannel"/> entries are kept per channel.
    /// </summary>
    public sealed class LogBuffer
    {
        public const int MaxEntriesPerChannel = 500;

        private static readonly IReadOnlyList<CompanionLogEntry> Empty = Array.Empty<CompanionLogEntry>();
        private readonly Dictionary<string, List<CompanionLogEntry>> entriesByChannel =
            new(StringComparer.OrdinalIgnoreCase);

        public int Add(CompanionLogEntry entry)
        {
            if (!entriesByChannel.TryGetValue(entry.ChannelId, out var entries))
            {
                entries = new List<CompanionLogEntry>();
                entriesByChannel[entry.ChannelId] = entries;
            }

            entries.Insert(0, entry);
            if (entries.Count > MaxEntriesPerChannel)
            {
                entries.RemoveRange(MaxEntriesPerChannel, entries.Count - MaxEntriesPerChannel);
            }

            return entries.Count;
        }

        public IReadOnlyList<CompanionLogEntry> GetEntries(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return Empty;
            }

            return entriesByChannel.TryGetValue(channelId, out var entries) ? entries : Empty;
        }

        public int GetCount(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return 0;
            }

            return entriesByChannel.TryGetValue(channelId, out var entries) ? entries.Count : 0;
        }

        public void Clear(string channelId)
        {
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                entriesByChannel.Remove(channelId);
            }
        }
    }
}
