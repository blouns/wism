using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Companion.Shared.Events;

namespace Wism.Companion.Shared.Telemetry
{
    public class TelemetryChannelRegistry
    {
        private readonly Dictionary<string, MapSnapshot> latestMaps = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<string> Channels => latestMaps.Keys.OrderBy(channel => channel).ToArray();

        public string Register(MapSnapshot snapshot)
        {
            var channel = TelemetryContext.ChannelIdOrDefault(snapshot.Telemetry);
            latestMaps[channel] = snapshot;
            return channel;
        }

        public MapSnapshot? GetLatestMap(string? channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return null;
            }

            return latestMaps.TryGetValue(channel, out var snapshot) ? snapshot : null;
        }
    }
}
