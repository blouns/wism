using Newtonsoft.Json;
using Wism.Companion.Shared.Events;

namespace Wism.CompanionApp.WinForms
{
    public class CommandLogger
    {
        private readonly List<object> _eventLog = new();
        private string? _channelId;

        public bool IsRecording { get; private set; }

        public void Start(string? channelId)
        {
            _channelId = channelId;
            IsRecording = true;
        }

        public void Stop() => IsRecording = false;

        public void Log(object evt)
        {
            if (IsRecording && MatchesSelectedChannel(evt))
            {
                _eventLog.Add(evt);
            }
        }

        public void Save(string filePath)
        {
            var json = JsonConvert.SerializeObject(_eventLog, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            File.WriteAllText(filePath, json);
        }

        public void Clear() => _eventLog.Clear();

        private bool MatchesSelectedChannel(object evt)
        {
            if (string.IsNullOrWhiteSpace(_channelId))
            {
                return false;
            }

            return evt switch
            {
                CommandExecutedEvent command => string.Equals(
                    TelemetryContext.ChannelIdOrDefault(command.Telemetry),
                    _channelId,
                    StringComparison.OrdinalIgnoreCase),
                MapSnapshot map => string.Equals(
                    TelemetryContext.ChannelIdOrDefault(map.Telemetry),
                    _channelId,
                    StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
