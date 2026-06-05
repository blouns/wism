using System;

namespace Wism.Companion.Shared.Events
{
    public class TelemetryContext
    {
        public const string DefaultChannelId = "default";
        public const string DefaultSessionId = "local";
        public const string DefaultSourceKind = "local";
        public const string DefaultSourceName = "local";

        public string ChannelId { get; set; } = DefaultChannelId;
        public string SessionId { get; set; } = DefaultSessionId;
        public string SourceKind { get; set; } = DefaultSourceKind;
        public string SourceName { get; set; } = DefaultSourceName;
        public string? RunId { get; set; }
        public string? InstanceId { get; set; }
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public static string ChannelIdOrDefault(TelemetryContext? context)
        {
            return string.IsNullOrWhiteSpace(context?.ChannelId)
                ? DefaultChannelId
                : context.ChannelId;
        }

        public static string SessionIdOrDefault(TelemetryContext? context)
        {
            return string.IsNullOrWhiteSpace(context?.SessionId)
                ? DefaultSessionId
                : context.SessionId;
        }
    }
}
