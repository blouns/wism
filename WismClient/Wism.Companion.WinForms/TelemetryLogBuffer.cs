namespace Wism.Companion.WinForms
{
    internal sealed class TelemetryLogBuffer
    {
        private const int MaxEntriesPerChannel = 500;
        private readonly Dictionary<string, List<TelemetryLogEntry>> entriesByChannel = new(StringComparer.OrdinalIgnoreCase);

        public int Add(TelemetryLogEntry entry)
        {
            if (!entriesByChannel.TryGetValue(entry.ChannelId, out var entries))
            {
                entries = new List<TelemetryLogEntry>();
                entriesByChannel[entry.ChannelId] = entries;
            }

            entries.Insert(0, entry);
            if (entries.Count > MaxEntriesPerChannel)
            {
                entries.RemoveRange(MaxEntriesPerChannel, entries.Count - MaxEntriesPerChannel);
            }

            return entries.Count;
        }

        public IReadOnlyList<TelemetryLogEntry> GetEntries(string? channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return Array.Empty<TelemetryLogEntry>();
            }

            return entriesByChannel.TryGetValue(channelId, out var entries)
                ? entries
                : Array.Empty<TelemetryLogEntry>();
        }

        public int GetCount(string channelId)
        {
            return entriesByChannel.TryGetValue(channelId, out var entries) ? entries.Count : 0;
        }

        public void Clear(string? channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return;
            }

            entriesByChannel.Remove(channelId);
        }
    }
}
