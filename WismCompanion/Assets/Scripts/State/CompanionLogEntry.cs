using System;
using System.Collections.Generic;
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

        private static readonly HashSet<string> BattleCommandTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "PrepareForBattleCommand", "AttackOnceCommand", "CompleteBattleCommand"
        };

        public static CompanionLogEntry FromCommand(CommandExecutedEvent command)
        {
            var channel = TelemetryContext.ChannelIdOrDefault(command.Telemetry);
            var actor = string.IsNullOrWhiteSpace(command.ActorId) ? "?" : command.ActorId;
            var target = command.TargetPosition != null
                ? $"({command.TargetPosition.X},{command.TargetPosition.Y})"
                : string.IsNullOrWhiteSpace(command.TargetId) ? "?" : command.TargetId;

            var isBattle = BattleCommandTypes.Contains(command.CommandType);
            var category = isBattle ? "Battle" : "Command";

            string summary, detail;
            if (isBattle)
            {
                summary = FormatBattleSummary(command.CommandType, actor, target, command.Result);
                detail  = FormatBattleDetail(command.CommandType, actor, target, command.Parameters, command.Result);
            }
            else
            {
                var parameters = command.Parameters == null || command.Parameters.Count == 0
                    ? "none"
                    : string.Join(", ", command.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                summary = $"{command.CommandType}: {actor} -> {target}";
                detail  = $"{command.CommandType}\nActor:  {actor}\nTarget: {target}\nParams: {parameters}\nResult: {command.Result ?? string.Empty}";
            }

            return new CompanionLogEntry(command.Timestamp, channel, category, summary, detail, command.Result);
        }

        private static string FormatBattleSummary(string type, string actor, string target, string result)
        {
            var outcome = string.IsNullOrWhiteSpace(result) ? string.Empty : $" [{result}]";
            return type switch
            {
                var t when t.Equals("PrepareForBattleCommand", StringComparison.OrdinalIgnoreCase)
                    => $"⚔ Battle begins: {actor} vs {target}",
                var t when t.Equals("AttackOnceCommand", StringComparison.OrdinalIgnoreCase)
                    => $"⚔ Strike: {actor} → {target}{outcome}",
                var t when t.Equals("CompleteBattleCommand", StringComparison.OrdinalIgnoreCase)
                    => $"⚔ Battle ended: {actor}{outcome}",
                _ => $"{type}: {actor} → {target}{outcome}"
            };
        }

        private static string FormatBattleDetail(string type, string actor, string target,
            Dictionary<string, object> parameters, string result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(type);
            sb.AppendLine($"Attacker: {actor}");
            sb.AppendLine($"Defender: {target}");
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var kvp in parameters)
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            if (!string.IsNullOrWhiteSpace(result))
                sb.AppendLine($"Outcome:  {result}");
            return sb.ToString().TrimEnd();
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
