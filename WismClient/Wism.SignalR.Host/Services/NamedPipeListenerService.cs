using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        public NamedPipeListenerService(ILogger<NamedPipeListenerService> logger, IHubContext<GameHub> hub)
        {
            _logger = logger;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting NamedPipe listener on {PipeName}", PipeName);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                _logger.LogInformation("Waiting for pipe connection...");
                await server.WaitForConnectionAsync(stoppingToken);

                _logger.LogInformation("Pipe client connected.");
                using var reader = new StreamReader(server, Encoding.UTF8);

                while (!stoppingToken.IsCancellationRequested && server.IsConnected)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        _logger.LogInformation("RAW JSON RECEIVED: " + line);

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

                _logger.LogInformation("Pipe disconnected.");
            }
        }
    }
}
