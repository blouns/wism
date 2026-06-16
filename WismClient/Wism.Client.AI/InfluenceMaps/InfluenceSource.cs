// File: Wism.Client.AI/InfluenceMaps/InfluenceSource.cs

namespace Wism.Client.AI.InfluenceMaps
{
    /// <summary>
    ///     A point source of influence: a <see cref="Strength"/> injected at (<see cref="X"/>,
    ///     <see cref="Y"/>) that floods outward across terrain it can traverse. The movement
    ///     flags gate which terrain the source's influence crosses (e.g. a naval stack projects
    ///     over water; a land stack does not).
    /// </summary>
    /// <remarks>
    ///     Sources are supplied explicitly to <c>ForwardFeedInfluenceMap.Compute</c> so the flood
    ///     can be unit-tested on hand-built maps without initializing a full <c>Game</c>.
    /// </remarks>
    public readonly struct InfluenceSource
    {
        public InfluenceSource(int x, int y, double strength, bool canWalk, bool canFloat, bool canFly)
        {
            this.X = x;
            this.Y = y;
            this.Strength = strength;
            this.CanWalk = canWalk;
            this.CanFloat = canFloat;
            this.CanFly = canFly;
        }

        public int X { get; }

        public int Y { get; }

        public double Strength { get; }

        public bool CanWalk { get; }

        public bool CanFloat { get; }

        public bool CanFly { get; }
    }
}
