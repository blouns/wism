using System;
using System.Diagnostics;

namespace Wism.Client.Diagnostics
{
    // Per-thread instrumentation only; no game state is shared with the output worker.
    public sealed class PerformanceProbe
    {
        [ThreadStatic] public static PerformanceProbe Current;
        private readonly Action<PerformanceSample> sink;
        private readonly Action<string> beginMarker;
        private readonly Action endMarker;
        private readonly long[] children = new long[64];
        private int depth;
        public long DecisionId { get; set; }
        public long PathQueries { get; private set; }
        public long ExpandedNodes { get; private set; }
        public long GraphCells { get; private set; }

        public PerformanceProbe(Action<PerformanceSample> sink, Action<string> beginMarker = null, Action endMarker = null)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            this.beginMarker = beginMarker;
            this.endMarker = endMarker;
        }

        public static Scope Measure(string name) => Current == null ? default : new Scope(Current, name);
        public void ResetPaths() { PathQueries = ExpandedNodes = GraphCells = 0; }
        public void PathStarted(int cells) { PathQueries++; GraphCells += cells; }
        public void PathExpanded() { ExpandedNodes++; }

        public struct Scope : IDisposable
        {
            private readonly PerformanceProbe owner;
            private readonly string name;
            private readonly long start;
            private readonly int level;
            internal Scope(PerformanceProbe owner, string name)
            {
                this.owner = owner;
                this.name = name;
                level = owner.depth++;
                if (level < owner.children.Length) owner.children[level] = 0;
                owner.beginMarker?.Invoke(name);
                start = Stopwatch.GetTimestamp();
            }
            public void Dispose()
            {
                if (owner == null) return;
                var elapsed = Stopwatch.GetTimestamp() - start;
                owner.endMarker?.Invoke();
                owner.depth--;
                if (level > 0 && level <= owner.children.Length) owner.children[level - 1] += elapsed;
                owner.sink(new PerformanceSample {
                    Kind = "scope", Name = name, Decision = owner.DecisionId, Timestamp = start,
                    DurationMs = elapsed * 1000d / Stopwatch.Frequency,
                    ExclusiveMs = level < owner.children.Length ? (elapsed - owner.children[level]) * 1000d / Stopwatch.Frequency : (double?)null
                });
            }
        }
    }

    public struct PerformanceSample
    {
        public string Kind, Name, Detail;
        public long Timestamp, Decision, Command;
        public double? DurationMs, ExclusiveMs, Value;
        public long? Count, Bytes;
        public double? MainThreadMs, RenderThreadMs, InputEventSeconds;
        public long? PathQueries, ExpandedNodes, GraphCells, Gc0, Gc1, Gc2;
    }
}
