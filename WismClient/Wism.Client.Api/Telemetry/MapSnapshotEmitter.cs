using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using Wism.Client.Common;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Api.Telemetry
{
    public class MapSnapshotEmitter
    {
        private NamedPipeClientStream? _pipe;
        private StreamWriter? _writer;
        private bool _connected = false;
        private readonly IWismLogger _logger;

        public MapSnapshotEmitter(IWismLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger();
        }

        public void Connect()
        {
            if (_connected) return;

            try
            {
                _pipe = new NamedPipeClientStream(".", "wism-commands", PipeDirection.Out);
                _pipe.Connect(200);
                _writer = new StreamWriter(_pipe) { AutoFlush = true };
                _connected = true;
                _logger.LogInformation("Connected to named pipe for map snapshots.");
            }
            catch (IOException ex)
            {
                _logger.LogWarning($"Failed to connect to map pipe: {ex.Message}");
            }
        }

        public void Publish(MapSnapshot snapshot)
        {
            try
            {
                Connect();

                if (!_connected || _writer == null)
                    return;

                var envelope = new
                {
                    Type = nameof(MapSnapshot),
                    Payload = snapshot
                };

                string json = JsonConvert.SerializeObject(envelope);
                _writer.WriteLine(json);

                //_logger.LogDebug("Map snapshot published.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to publish map snapshot: {ex.Message}");
            }
        }
    }
}
