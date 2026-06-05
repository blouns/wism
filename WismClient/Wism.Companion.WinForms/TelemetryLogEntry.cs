using Wism.Companion.Shared.Events;

namespace Wism.Companion.WinForms
{
    public sealed class TelemetryLogEntry
    {
        public TelemetryLogEntry(
            DateTime timestampUtc,
            string channelId,
            string category,
            string summary,
            string detail,
            string? result = null)
        {
            TimestampUtc = timestampUtc;
            ChannelId = channelId;
            Category = category;
            Summary = summary;
            Detail = detail;
            Result = result ?? string.Empty;
        }

        public DateTime TimestampUtc { get; }
        public string ChannelId { get; }
        public string Category { get; }
        public string Summary { get; }
        public string Detail { get; }
        public string Result { get; }

        public string LocalTime => TimestampUtc.ToLocalTime().ToString("HH:mm:ss");

        public static TelemetryLogEntry FromCommand(CommandExecutedEvent command)
        {
            var channel = TelemetryContext.ChannelIdOrDefault(command.Telemetry);
            var actor = string.IsNullOrWhiteSpace(command.ActorId) ? "unknown actor" : command.ActorId;
            var target = command.TargetPosition is not null
                ? $"({command.TargetPosition.X},{command.TargetPosition.Y})"
                : string.IsNullOrWhiteSpace(command.TargetId) ? "no target" : command.TargetId;
            var parameters = command.Parameters.Count == 0
                ? "no params"
                : string.Join(", ", command.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            return new TelemetryLogEntry(
                command.Timestamp,
                channel,
                "Command",
                $"{command.CommandType}: {actor} -> {target}",
                $"{command.CommandType}\r\nActor: {actor}\r\nTarget: {target}\r\nParameters: {parameters}\r\nResult: {command.Result ?? string.Empty}",
                command.Result);
        }

        public static TelemetryLogEntry FromMap(MapSnapshot map)
        {
            var channel = TelemetryContext.ChannelIdOrDefault(map.Telemetry);

            return new TelemetryLogEntry(
                map.Timestamp,
                channel,
                "Map",
                $"{map.Width}x{map.Height}, {map.Armies.Count} armies, {map.Cities.Count} cities",
                $"Map snapshot\r\nSize: {map.Width}x{map.Height}\r\nArmies: {map.Armies.Count}\r\nCities: {map.Cities.Count}\r\nLocations: {map.Locations.Count}\r\nItems: {map.Items.Count}");
        }

        public static TelemetryLogEntry Replay(object evt)
        {
            if (evt is CommandExecutedEvent command)
            {
                var entry = FromCommand(command);
                return new TelemetryLogEntry(
                    entry.TimestampUtc,
                    entry.ChannelId,
                    "Replay Command",
                    entry.Summary,
                    entry.Detail,
                    entry.Result);
            }

            if (evt is MapSnapshot map)
            {
                var entry = FromMap(map);
                return new TelemetryLogEntry(
                    entry.TimestampUtc,
                    entry.ChannelId,
                    "Replay Map",
                    entry.Summary,
                    entry.Detail,
                    entry.Result);
            }

            return new TelemetryLogEntry(
                DateTime.UtcNow,
                TelemetryContext.DefaultChannelId,
                "Replay",
                "Unknown event",
                evt.GetType().FullName ?? "unknown type");
        }
    }
}
