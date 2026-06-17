// File: Wism.Client.AI/InfluenceMaps/ForwardFeedInfluenceMap.cs

using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.InfluenceMaps
{
    /// <summary>
    ///     Terrain-aware, two-channel forward-feed influence map (Recommendation 1 / Workstream A
    ///     prototype). Each channel (friendly, enemy) is flooded once with a single terrain-weighted
    ///     multi-source Dijkstra sweep: a cell's influence is the strength of the nearest reachable
    ///     source attenuated by terrain-cost distance (the "dominant nearest source" model). Because
    ///     the flood travels along real movement edges (<see cref="Terrain.MovementCost"/>, gated by
    ///     <see cref="Terrain.CanTraverse(bool, bool, bool)"/>), the field encodes roads, water,
    ///     mountains, and chokepoints — unlike the Manhattan distance ADAPTA uses today.
    /// </summary>
    /// <remarks>
    ///     Design notes (see whitepaper §4): the field is keyed by (x, y); channels are calibrated
    ///     against a fixed reference (a lone scout never normalizes to look like an army); momentum
    ///     is off by default (<see cref="Momentum"/> = 1.0) for deterministic evals. Additive
    ///     superposition and per-clan sub-channel merging are out of scope for this prototype.
    /// </remarks>
    public sealed class ForwardFeedInfluenceMap : ISpatialAdvisor
    {
        /// <summary>Flat ownership term so even an empty owned city projects some control.</summary>
        private const double CityOwnershipBonus = 2.0;

        /// <summary>Flood stops once decay falls below this; bounds the sweep radius.</summary>
        private const double CutoffEpsilon = 0.02;

        /// <summary>Tension magnitude below which a sign change is treated as noise, not a front.</summary>
        private const double FrontEpsilon = 0.02;

        private int width;
        private int height;
        private Tile[,] map;

        private double[,] friendlyRaw;
        private double[,] enemyRaw;
        private double[,] friendlyCal;
        private double[,] enemyCal;

        // Momentum (live play only): the previous turn's calibrated channels.
        private double[,] priorFriendlyCal;
        private double[,] priorEnemyCal;

        /// <summary>Exponential decay rate per unit of movement-cost distance.</summary>
        public double Lambda { get; set; } = 0.35;

        /// <summary>
        ///     Temporal smoothing factor. 1.0 = momentum off (deterministic, eval-safe); values in
        ///     (0, 1) blend in the previous turn's field for front stability in live play.
        /// </summary>
        public double Momentum { get; set; } = 1.0;

        /// <summary>
        ///     Fixed calibration denominator: a reference "strong stack" strength held constant
        ///     across turns and games, so calibrated influence is comparable over time.
        /// </summary>
        public double CalibrationReference { get; set; } = Army.MaxStrength * Army.MaxArmies;

        /// <summary>
        ///     Rebuild the field from the live game state (current player = friendly, all other
        ///     living players = enemy). Reads <see cref="World.Current"/> / <see cref="Game.Current"/>.
        /// </summary>
        public void Update()
        {
            var game = Game.Current;
            var world = World.Current;
            if (game == null || world?.Map == null)
            {
                // No live game/map (e.g. a turn driven without world state): leave the field empty
                // rather than throwing, so the per-turn wiring is safe to call unconditionally.
                return;
            }

            var me = game.GetCurrentPlayer();

            var friendly = new List<InfluenceSource>();
            var enemy = new List<InfluenceSource>();

            foreach (var player in game.Players)
            {
                if (player == null || player.IsDead)
                {
                    continue;
                }

                var bucket = player == me ? friendly : enemy;
                CollectArmySources(player, bucket);
                CollectCitySources(player, bucket);
            }

            this.Compute(world.Map, friendly, enemy);
        }

        /// <summary>
        ///     Compute the field directly from a map and explicit source lists. This is the
        ///     test/eval-friendly entry point: it touches no global game state.
        /// </summary>
        public void Compute(Tile[,] map, IReadOnlyList<InfluenceSource> friendlySources, IReadOnlyList<InfluenceSource> enemySources)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.width = map.GetLength(0);
            this.height = map.GetLength(1);

            this.friendlyRaw = this.FloodChannel(friendlySources);
            this.enemyRaw = this.FloodChannel(enemySources);

            this.friendlyCal = this.Calibrate(this.friendlyRaw);
            this.enemyCal = this.Calibrate(this.enemyRaw);

            this.ApplyMomentum();
        }

        public double GetFriendly(Tile tile) => tile == null ? 0.0 : this.GetFriendly(tile.X, tile.Y);

        public double GetEnemy(Tile tile) => tile == null ? 0.0 : this.GetEnemy(tile.X, tile.Y);

        public double GetTension(Tile tile) => tile == null ? 0.0 : this.GetTension(tile.X, tile.Y);

        public double GetRawFriendly(Tile tile) => this.InBounds(tile) ? this.friendlyRaw[tile.X, tile.Y] : 0.0;

        public double GetRawEnemy(Tile tile) => this.InBounds(tile) ? this.enemyRaw[tile.X, tile.Y] : 0.0;

        public double GetFriendly(int x, int y) => this.InBounds(x, y) ? this.friendlyCal[x, y] : 0.0;

        public double GetEnemy(int x, int y) => this.InBounds(x, y) ? this.enemyCal[x, y] : 0.0;

        public double GetTension(int x, int y) => this.GetFriendly(x, y) - this.GetEnemy(x, y);

        /// <summary>
        ///     <see cref="IInfluenceMap"/> compatibility: returns calibrated enemy influence at the
        ///     tile (higher near enemy force), matching the enemy-proximity sense of the old stub.
        /// </summary>
        public double GetInfluence(Tile tile) => this.GetEnemy(tile);

        public bool IsFrontLine(Tile tile)
        {
            if (!this.InBounds(tile))
            {
                return false;
            }

            var here = this.GetTension(tile.X, tile.Y);
            var herePositive = here >= 0.0;

            foreach (var n in this.Neighbors(tile.X, tile.Y))
            {
                var there = this.GetTension(n.X, n.Y);
                if ((there >= 0.0) != herePositive && Math.Max(Math.Abs(here), Math.Abs(there)) > FrontEpsilon)
                {
                    return true;
                }
            }

            return false;
        }

        public Tile GetGradientStep(Tile from, bool ascendFriendly)
        {
            if (!this.InBounds(from))
            {
                return from;
            }

            var best = from;
            var bestValue = ascendFriendly
                ? this.GetFriendly(from.X, from.Y)
                : this.GetEnemy(from.X, from.Y);

            foreach (var n in this.Neighbors(from.X, from.Y))
            {
                var value = ascendFriendly ? this.GetFriendly(n.X, n.Y) : this.GetEnemy(n.X, n.Y);
                if (value > bestValue)
                {
                    bestValue = value;
                    best = n;
                }
            }

            return best;
        }

        /// <summary>
        ///     Single terrain-weighted multi-source Dijkstra. Each cell carries the strength and
        ///     movement mask of the dominant (nearest) source reaching it; raw influence at a cell
        ///     is that strength attenuated by exp(-lambda * costDistance).
        /// </summary>
        private double[,] FloodChannel(IReadOnlyList<InfluenceSource> sources)
        {
            var raw = new double[this.width, this.height];
            if (sources == null || sources.Count == 0)
            {
                return raw;
            }

            var dist = new double[this.width, this.height];
            var ownerStrength = new double[this.width, this.height];
            var ownerWalk = new bool[this.width, this.height];
            var ownerFloat = new bool[this.width, this.height];
            var ownerFly = new bool[this.width, this.height];

            for (var x = 0; x < this.width; x++)
            {
                for (var y = 0; y < this.height; y++)
                {
                    dist[x, y] = double.PositiveInfinity;
                }
            }

            // Frontier ordered by (cost, x, y) for deterministic tie-breaking.
            var frontier = new SortedSet<FrontierKey>();

            // Seed: co-located sources aggregate (a stack sums strength; masks OR together).
            foreach (var source in sources)
            {
                if (!this.InBounds(source.X, source.Y))
                {
                    continue;
                }

                var sx = source.X;
                var sy = source.Y;
                if (dist[sx, sy] != 0.0)
                {
                    dist[sx, sy] = 0.0;
                    frontier.Add(new FrontierKey(0.0, sx, sy));
                }

                ownerStrength[sx, sy] += source.Strength;
                ownerWalk[sx, sy] |= source.CanWalk;
                ownerFloat[sx, sy] |= source.CanFloat;
                ownerFly[sx, sy] |= source.CanFly;
            }

            var maxCostDistance = this.Lambda > 0.0
                ? -Math.Log(CutoffEpsilon) / this.Lambda
                : double.PositiveInfinity;

            while (frontier.Count > 0)
            {
                var current = frontier.Min;
                frontier.Remove(current);

                var cx = current.X;
                var cy = current.Y;
                var cd = current.Cost;

                foreach (var n in this.Neighbors(cx, cy))
                {
                    var terrain = n.Terrain;
                    if (terrain == null ||
                        !terrain.CanTraverse(ownerWalk[cx, cy], ownerFloat[cx, cy], ownerFly[cx, cy]))
                    {
                        continue;
                    }

                    // Flying pays 1 per tile regardless of terrain (Warlords rule).
                    var step = ownerFly[cx, cy] ? 1.0 : terrain.MovementCost;
                    var nd = cd + step;
                    if (nd > maxCostDistance)
                    {
                        continue;
                    }

                    if (nd < dist[n.X, n.Y])
                    {
                        if (!double.IsPositiveInfinity(dist[n.X, n.Y]))
                        {
                            frontier.Remove(new FrontierKey(dist[n.X, n.Y], n.X, n.Y));
                        }

                        dist[n.X, n.Y] = nd;
                        ownerStrength[n.X, n.Y] = ownerStrength[cx, cy];
                        ownerWalk[n.X, n.Y] = ownerWalk[cx, cy];
                        ownerFloat[n.X, n.Y] = ownerFloat[cx, cy];
                        ownerFly[n.X, n.Y] = ownerFly[cx, cy];
                        frontier.Add(new FrontierKey(nd, n.X, n.Y));
                    }
                }
            }

            for (var x = 0; x < this.width; x++)
            {
                for (var y = 0; y < this.height; y++)
                {
                    if (!double.IsPositiveInfinity(dist[x, y]))
                    {
                        raw[x, y] = ownerStrength[x, y] * Math.Exp(-this.Lambda * dist[x, y]);
                    }
                }
            }

            return raw;
        }

        private double[,] Calibrate(double[,] rawField)
        {
            var calibrated = new double[this.width, this.height];
            var denominator = this.CalibrationReference > 0.0 ? this.CalibrationReference : 1.0;

            for (var x = 0; x < this.width; x++)
            {
                for (var y = 0; y < this.height; y++)
                {
                    var value = rawField[x, y] / denominator;
                    calibrated[x, y] = value > 1.0 ? 1.0 : value;
                }
            }

            return calibrated;
        }

        private void ApplyMomentum()
        {
            var alpha = this.Momentum;
            if (alpha < 1.0 &&
                this.priorFriendlyCal != null &&
                this.priorFriendlyCal.GetLength(0) == this.width &&
                this.priorFriendlyCal.GetLength(1) == this.height)
            {
                for (var x = 0; x < this.width; x++)
                {
                    for (var y = 0; y < this.height; y++)
                    {
                        this.friendlyCal[x, y] = (alpha * this.friendlyCal[x, y]) + ((1.0 - alpha) * this.priorFriendlyCal[x, y]);
                        this.enemyCal[x, y] = (alpha * this.enemyCal[x, y]) + ((1.0 - alpha) * this.priorEnemyCal[x, y]);
                    }
                }
            }

            this.priorFriendlyCal = (double[,])this.friendlyCal.Clone();
            this.priorEnemyCal = (double[,])this.enemyCal.Clone();
        }

        private void CollectArmySources(Player player, List<InfluenceSource> bucket)
        {
            // Group armies into stacks by tile: a stack is one source summing strength, OR-ing masks.
            var stacks = new Dictionary<(int, int), InfluenceAccumulator>();

            foreach (var army in player.GetArmies())
            {
                if (army == null || army.IsDead || army.Tile == null)
                {
                    continue;
                }

                var key = (army.Tile.X, army.Tile.Y);
                if (!stacks.TryGetValue(key, out var acc))
                {
                    acc = new InfluenceAccumulator(army.Tile.X, army.Tile.Y);
                }

                acc.Strength += army.Strength;
                acc.CanWalk |= army.CanWalk;
                acc.CanFloat |= army.CanFloat;
                acc.CanFly |= army.CanFly;
                stacks[key] = acc;
            }

            foreach (var acc in stacks.Values)
            {
                bucket.Add(new InfluenceSource(acc.X, acc.Y, acc.Strength, acc.CanWalk, acc.CanFloat, acc.CanFly));
            }
        }

        private void CollectCitySources(Player player, List<InfluenceSource> bucket)
        {
            foreach (var city in player.GetCities())
            {
                if (city?.Tile == null)
                {
                    continue;
                }

                // A city projects garrison-style land control.
                bucket.Add(new InfluenceSource(city.Tile.X, city.Tile.Y, city.Defense + CityOwnershipBonus, true, false, false));
            }
        }

        private IEnumerable<Tile> Neighbors(int x, int y)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var nx = x + dx;
                    var ny = y + dy;
                    if (this.InBounds(nx, ny) && this.map[nx, ny] != null)
                    {
                        yield return this.map[nx, ny];
                    }
                }
            }
        }

        private bool InBounds(Tile tile) => tile != null && this.InBounds(tile.X, tile.Y);

        private bool InBounds(int x, int y) =>
            this.map != null && x >= 0 && y >= 0 && x < this.width && y < this.height;

        private struct InfluenceAccumulator
        {
            public InfluenceAccumulator(int x, int y)
            {
                this.X = x;
                this.Y = y;
                this.Strength = 0.0;
                this.CanWalk = false;
                this.CanFloat = false;
                this.CanFly = false;
            }

            public int X { get; }

            public int Y { get; }

            public double Strength { get; set; }

            public bool CanWalk { get; set; }

            public bool CanFloat { get; set; }

            public bool CanFly { get; set; }
        }

        /// <summary>Deterministic frontier key: ordered by cost, then x, then y.</summary>
        private readonly struct FrontierKey : IComparable<FrontierKey>
        {
            public FrontierKey(double cost, int x, int y)
            {
                this.Cost = cost;
                this.X = x;
                this.Y = y;
            }

            public double Cost { get; }

            public int X { get; }

            public int Y { get; }

            public int CompareTo(FrontierKey other)
            {
                var byCost = this.Cost.CompareTo(other.Cost);
                if (byCost != 0)
                {
                    return byCost;
                }

                var byX = this.X.CompareTo(other.X);
                return byX != 0 ? byX : this.Y.CompareTo(other.Y);
            }
        }
    }
}
