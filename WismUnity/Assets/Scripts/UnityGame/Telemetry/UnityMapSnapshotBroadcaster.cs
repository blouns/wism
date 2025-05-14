using Wism.Client.Api.Telemetry;
using Wism.Client.Core.Telemetry;

namespace Assets.Scripts.Telemetry
{
    public class UnityMapSnapshotBroadcaster : IMapSnapshotBroadcaster
    {
        private readonly MapSnapshotBuilder builder;
        private readonly MapSnapshotEmitter emitter;

        public UnityMapSnapshotBroadcaster(MapSnapshotBuilder builder, MapSnapshotEmitter emitter)
        {
            this.builder = builder;
            this.emitter = emitter;
        }

        public void TryEmitSnapshot()
        {
            if (builder.TryBuild(out var snapshot))
            {
                snapshot.InvertYAxis = true;

                emitter.Publish(snapshot);
            }
        }
    }
}
