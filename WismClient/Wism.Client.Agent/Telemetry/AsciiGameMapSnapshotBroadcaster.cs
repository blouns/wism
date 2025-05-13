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
                // flip only in ASCII due to flipped coordinate system
                snapshot = snapshot.FlipYAxis();
                emitter.Publish(snapshot);
            }
        }
    }
}
