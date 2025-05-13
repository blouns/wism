using Microsoft.AspNetCore.SignalR.Client;
using Wism.Companion.Shared.Events;

namespace Wism.CompanionApp.WinForms
{
    public class SignalRClient
    {
        private HubConnection _hub;
        private readonly Action<string> _logCallback;
        private readonly Action<object> _recordCallback;
        private readonly Action<MapSnapshot> _mapCallback;

        public SignalRClient(Action<string> logCallback, Action<object> recordCallback, Action<MapSnapshot> mapCallback)
        {
            _logCallback = logCallback;
            _recordCallback = recordCallback;
            _mapCallback = mapCallback;
        }


        public async Task ConnectAsync()
        {
            _hub = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/gameHub")
                .WithAutomaticReconnect()
                .Build();

            _hub.On<CommandExecutedEvent>("OnCommandExecuted", (cmd) =>
            {
                _logCallback?.Invoke($"[COMMAND] {cmd.CommandType} → {cmd.Result} @ {cmd.Timestamp:T}");
                _recordCallback?.Invoke(cmd);
            });

            _hub.On<MapSnapshot>("OnMapSnapshot", (map) =>
            {
                _logCallback?.Invoke($"[MAP] {map.Width}x{map.Height} with {map.Armies.Count} armies");
                _recordCallback?.Invoke(map);
                _mapCallback?.Invoke(map);
            });

            await _hub.StartAsync();
            _logCallback?.Invoke("[SignalR] Connected.");
        }
    }
}
