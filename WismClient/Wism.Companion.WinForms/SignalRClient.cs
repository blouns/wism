using Microsoft.AspNetCore.SignalR.Client;
using Wism.Companion.Shared.Events;
using Wism.Companion.WinForms;

namespace Wism.CompanionApp.WinForms
{
    public class SignalRClient
    {
        private HubConnection? _hub;
        private readonly Action<string> _statusCallback;
        private readonly Action<TelemetryLogEntry> _logCallback;
        private readonly Action<object> _recordCallback;
        private readonly Action<MapSnapshot> _mapCallback;
        private readonly Action<CommandExecutedEvent> _commandCallback;

        public SignalRClient(
            Action<string> statusCallback,
            Action<TelemetryLogEntry> logCallback,
            Action<object> recordCallback,
            Action<MapSnapshot> mapCallback,
            Action<CommandExecutedEvent> commandCallback)
        {
            _statusCallback = statusCallback;
            _logCallback = logCallback;
            _recordCallback = recordCallback;
            _mapCallback = mapCallback;
            _commandCallback = commandCallback;
        }

        public async Task ConnectAsync()
        {
            _hub = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/gameHub")
                .WithAutomaticReconnect()
                .Build();

            _hub.On<CommandExecutedEvent>("OnCommandExecuted", (cmd) =>
            {
                _logCallback.Invoke(TelemetryLogEntry.FromCommand(cmd));
                _recordCallback.Invoke(cmd);
                _commandCallback.Invoke(cmd);
            });

            _hub.On<MapSnapshot>("OnMapSnapshot", (map) =>
            {
                _logCallback.Invoke(TelemetryLogEntry.FromMap(map));
                _recordCallback.Invoke(map);
                _mapCallback.Invoke(map);
            });

            await _hub.StartAsync();
            _statusCallback.Invoke("Connected to SignalR host");
        }
    }
}
