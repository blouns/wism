using Wism.Client.Common;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Api.Telemetry
{
    public class CommandIpcPublisher
    {
        private readonly ITelemetryPublisher publisher;

        public CommandIpcPublisher(
            IWismLoggerFactory loggerFactory,
            TelemetryContext telemetryContext = null,
            ITelemetryPublisher publisher = null)
        {
            this.publisher = publisher ?? new NamedPipeTelemetryPublisher(loggerFactory, telemetryContext);
        }

        public void Publish(CommandExecutedEvent evt)
        {
            publisher.Publish(evt);
        }
    }
}
