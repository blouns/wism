using System;
using System.Linq;
using Wism.Companion.Shared.Events;

namespace WismCompanion.State
{
    /// <summary>
    /// Presentation-side log entry for the companion. Ported from the WinForms companion's
    /// TelemetryLogEntry so the Unity view shows the same time/kind/summary/detail/result shape.
    /// </summary>
    public sealed class CompanionLogEntry
    {
        public CompanionLogEntry(
            DateTime timestampUtc,
            string channelId,
            string category,
            string summary,
            string detail,
            string result = null)
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

        public static CompanionLogEntry FromCommand(CommandExecutedEvent command)
        {
            var channel = TelemetryContext.ChannelIdOrDefault(command.Telemetry);
            var actor = string.IsNullOrWhiteSpace(command.ActorId) ? "unknown actor" : command.ActorId;
            var target = command.TargetPosition != null
                ? $"({command.TargetPosition.X},{command.TargetPosition.Y})"
                : string.IsNullOrWhiteSpace(command.TargetId) ? "no target" : command.TargetId;
            var parameters = command.Parameters == null || command.Parameters.Count == 0
                ? "no params"
                : string.Join(", ", command.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            return new CompanionLogEntry(
                command.Timestamp,
                channel,
                "Command",
                $"{command.CommandType}: {actor} -> {target}",
                $"{command.CommandType}\nActor: {actor}\nTarget: {target}\nParameters: {parameters}\nResult: {command.Result ?? string.Empty}",
                command.Result);
        }

        public static CompanionLogEntry FromMap(MapSnapshot map)
        {
            var channel = TelemetryContext.ChannelIdOrDefault(map.Telemetry);

            return new CompanionLogEntry(
                map.Timestamp,
                channel,
                "Map",
                $"{map.Width}x{map.Height}, {map.Armies.Count} armies, {map.Cities.Count} cities",
                $"Map snapshot\nSize: {map.Width}x{map.Height}\nArmies: {map.Armies.Count}\nCities: {map.Cities.Count}\nLocations: {map.Locations.Count}\nItems: {map.Items.Count}");
        }
    }
}
