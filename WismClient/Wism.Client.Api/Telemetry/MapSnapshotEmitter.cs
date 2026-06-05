using Wism.Client.Common;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Api.Telemetry
{
    public class MapSnapshotEmitter
    {
        private readonly ITelemetryPublisher publisher;

        public MapSnapshotEmitter(
            IWismLoggerFactory loggerFactory,
            TelemetryContext telemetryContext = null,
            ITelemetryPublisher publisher = null)
        {
            this.publisher = publisher ?? new NamedPipeTelemetryPublisher(loggerFactory, telemetryContext);
        }

        public void Publish(MapSnapshot snapshot)
        {
            publisher.Publish(snapshot);
        }
    }
}
