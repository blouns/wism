using Wism.Client.Api.Telemetry;
using Wism.Client.Core.Telemetry;

namespace Wism.Client.Agent.Telemetry
{
    public class AsciiGameMapSnapshotBroadcaster : IMapSnapshotBroadcaster
    {
        private readonly MapSnapshotBuilder builder;
        private readonly MapSnapshotEmitter emitter;

        public AsciiGameMapSnapshotBroadcaster(MapSnapshotBuilder builder, MapSnapshotEmitter emitter)
        {
            this.builder = builder;
            this.emitter = emitter;
        }

        public void TryEmitSnapshot()
        {
            if (builder.TryBuild(out var snapshot))
            {
                // Mark for inversion in the companion renderer
                snapshot.InvertYAxis = true;

                emitter.Publish(snapshot);
            }
        }
    }
}
