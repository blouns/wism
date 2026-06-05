using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Wism.Companion.Shared.Events;
using Wism.SignalR.Host.Hubs;

namespace Wism.SignalR.Host.Services
{
    public class NamedPipeListenerService : BackgroundService
    {
        private readonly ILogger<NamedPipeListenerService> _logger;
        private readonly IHubContext<GameHub> _hub;
        private const string PipeName = "wism-commands";
        private const int ListenerCount = 16;

        public NamedPipeListenerService(ILogger<NamedPipeListenerService> logger, IHubContext<GameHub> hub)
    {
        _logger = logger;
        _hub = hub;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting {ListenerCount} NamedPipe listener loops on {PipeName}", ListenerCount, PipeName);
        var listeners = Enumerable
            .Range(0, ListenerCount)
            .Select(_ => Task.Run(() => ListenLoop(stoppingToken), stoppingToken));
        return Task.WhenAll(listeners);
    }

    private async Task ListenLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // allow up to 2 simultaneous clients
            using var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.In,
                ListenerCount,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            _logger.LogInformation("[{Instance}] Waiting for pipe connection", server.GetHashCode());
            await server.WaitForConnectionAsync(stoppingToken);
            _logger.LogInformation("[{Instance}] Client connected", server.GetHashCode());

            await ProcessMessages(server, stoppingToken);
            _logger.LogInformation("[{Instance}] Client disconnected", server.GetHashCode());
        }
    }

        private async Task ProcessMessages(NamedPipeServerStream server, CancellationToken stoppingToken)
        {
            using var reader = new StreamReader(server, Encoding.UTF8);

            while (!stoppingToken.IsCancellationRequested && server.IsConnected)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    _logger.LogInformation("RAW JSON RECEIVED: {Line}", line);
                    var doc = JsonDocument.Parse(line);
                    var type = doc.RootElement.GetProperty("Type").GetString();

                    if (type == nameof(CommandExecutedEvent))
                    {
                        var payloadJson = doc.RootElement.GetProperty("Payload").GetRawText();
                        var evt = JsonConvert.DeserializeObject<CommandExecutedEvent>(
                            payloadJson,
                            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                        await _hub.Clients.All.SendAsync("OnCommandExecuted", evt, stoppingToken);
                        _logger.LogInformation("Broadcasted CommandExecutedEvent");
                    }
                    else if (type == nameof(MapSnapshot))
                    {
                        var payloadJson = doc.RootElement.GetProperty("Payload").GetRawText();
                        var snapshot = JsonConvert.DeserializeObject<MapSnapshot>(payloadJson);
                        await _hub.Clients.All.SendAsync("OnMapSnapshot", snapshot, stoppingToken);
                        _logger.LogInformation("Broadcasted MapSnapshot");
                    }
                    else
                    {
                        _logger.LogWarning("Unknown event type: {Type}", type);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process pipe message");
                }
            }
        }
    }

}
