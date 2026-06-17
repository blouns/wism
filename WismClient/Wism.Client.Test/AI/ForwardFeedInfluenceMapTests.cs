using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Test.AI
{
    /// <summary>
    ///     Prototype acceptance probes for the forward-feed influence map (Recommendation 1).
    ///     These build hand-crafted <see cref="Tile"/> grids and explicit sources, so the flood is
    ///     exercised without initializing a full <see cref="Game"/>.
    /// </summary>
    [TestFixture]
    public class ForwardFeedInfluenceMapTests
    {
        private const double Weak = 4.0;     // a lone scout
        private const double Strong = 72.0;  // a max stack (MaxStrength * MaxArmies)

        private static readonly InfluenceSource[] NoSources = new InfluenceSource[0];

        [Test]
        public void Influence_ReachesFartherAlongRoads_ThanRoughTerrain()
        {
            var roadMap = FillMap(7, 3, MakeTerrain("road", movement: 1, walk: true));
            var roughMap = FillMap(7, 3, MakeTerrain("rough", movement: 4, walk: true));

            var roadField = new ForwardFeedInfluenceMap();
            roadField.Compute(roadMap, new[] { Land(0, 1, Strong) }, NoSources);

            var roughField = new ForwardFeedInfluenceMap();
            roughField.Compute(roughMap, new[] { Land(0, 1, Strong) }, NoSources);

            // Same tile distance, but the road carries influence much farther than rough terrain.
            Assert.That(roadField.GetRawFriendly(roadMap[6, 1]), Is.GreaterThan(0.0));
            Assert.That(roadField.GetRawFriendly(roadMap[6, 1]),
                Is.GreaterThan(roughField.GetRawFriendly(roughMap[6, 1])));
        }

        [Test]
        public void Influence_DoesNotBleedThroughImpassableTerrain()
        {
            // Full-height mountain wall at x = 2 separates the left half from the right half.
            var map = FillMap(5, 3, MakeTerrain("road", movement: 1, walk: true));
            var mountain = MakeTerrain("mountain", movement: 99, walk: false, fly: true);
            for (var y = 0; y < 3; y++)
            {
                map[2, y].Terrain = mountain;
            }

            var field = new ForwardFeedInfluenceMap();
            field.Compute(map, new[] { Land(0, 1, Strong) }, NoSources);

            Assert.That(field.GetRawFriendly(map[1, 1]), Is.GreaterThan(0.0), "near side reachable");
            Assert.That(field.GetRawFriendly(map[3, 1]), Is.EqualTo(0.0), "wall blocks the flood");
            Assert.That(field.GetRawFriendly(map[4, 1]), Is.EqualTo(0.0), "far side unreachable by land");
        }

        [Test]
        public void Influence_RespectsMovementMode_WaterLandSplit()
        {
            // Coast allows walk + float; a deep-water column at x = 2 allows float only.
            var coast = MakeTerrain("coast", movement: 1, walk: true, float_: true);
            var deepWater = MakeTerrain("water", movement: 1, walk: false, float_: true);

            var landMap = FillMap(5, 3, coast);
            var navalMap = FillMap(5, 3, coast);
            for (var y = 0; y < 3; y++)
            {
                landMap[2, y].Terrain = deepWater;
                navalMap[2, y].Terrain = deepWater;
            }

            var landField = new ForwardFeedInfluenceMap();
            landField.Compute(landMap, new[] { Source(0, 1, Strong, walk: true) }, NoSources);

            var navalField = new ForwardFeedInfluenceMap();
            navalField.Compute(navalMap, new[] { Source(0, 1, Strong, float_: true) }, NoSources);

            // Land influence cannot cross the deep water; naval influence can.
            Assert.That(landField.GetRawFriendly(landMap[4, 1]), Is.EqualTo(0.0), "land blocked by water");
            Assert.That(navalField.GetRawFriendly(navalMap[4, 1]), Is.GreaterThan(0.0), "naval crosses water");
        }

        [Test]
        public void Calibration_PreservesMagnitude_ScoutIsNotAnArmy()
        {
            var map = FillMap(5, 3, MakeTerrain("grass", movement: 1, walk: true));

            var weakField = new ForwardFeedInfluenceMap();
            weakField.Compute(map, new[] { Land(2, 1, Weak) }, NoSources);

            var strongField = new ForwardFeedInfluenceMap();
            strongField.Compute(map, new[] { Land(2, 1, Strong) }, NoSources);

            var scout = weakField.GetFriendly(map[2, 1]);
            var army = strongField.GetFriendly(map[2, 1]);

            Assert.That(scout, Is.LessThan(army), "a scout must not calibrate up to an army");
            Assert.That(scout, Is.LessThan(0.2));
            Assert.That(army, Is.EqualTo(1.0).Within(1e-9), "a max stack saturates the calibrated channel");
        }

        [Test]
        public void Tension_MarksFrontLine_BetweenOpposingSources()
        {
            var map = FillMap(5, 3, MakeTerrain("grass", movement: 1, walk: true));

            var field = new ForwardFeedInfluenceMap();
            field.Compute(map, new[] { Land(0, 1, Strong) }, new[] { Land(4, 1, Strong) });

            Assert.That(field.GetTension(0, 1), Is.GreaterThan(0.0), "friendly side is positive");
            Assert.That(field.GetTension(4, 1), Is.LessThan(0.0), "enemy side is negative");
            Assert.That(field.IsFrontLine(map[2, 1]), Is.True, "midpoint is a contested front");

            // Gradient points back toward friendly support, or forward toward the enemy.
            Assert.That(field.GetGradientStep(map[2, 1], ascendFriendly: true).X, Is.EqualTo(1));
            Assert.That(field.GetGradientStep(map[2, 1], ascendFriendly: false).X, Is.EqualTo(3));
        }

        [Test]
        public void Compute_IsDeterministic_ForIdenticalState()
        {
            var map = FillMap(6, 4, MakeTerrain("grass", movement: 2, walk: true));
            var friendly = new[] { Land(0, 0, Strong), Land(1, 3, Weak) };
            var enemy = new[] { Land(5, 3, Strong) };

            var first = new ForwardFeedInfluenceMap();
            first.Compute(map, friendly, enemy);

            var second = new ForwardFeedInfluenceMap();
            second.Compute(map, friendly, enemy);

            for (var x = 0; x < 6; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    Assert.That(second.GetFriendly(x, y), Is.EqualTo(first.GetFriendly(x, y)));
                    Assert.That(second.GetEnemy(x, y), Is.EqualTo(first.GetEnemy(x, y)));
                }
            }
        }

        private static InfluenceSource Land(int x, int y, double strength) =>
            new InfluenceSource(x, y, strength, canWalk: true, canFloat: false, canFly: false);

        private static InfluenceSource Source(int x, int y, double strength, bool walk = false, bool float_ = false, bool fly = false) =>
            new InfluenceSource(x, y, strength, walk, float_, fly);

        private static Tile[,] FillMap(int width, int height, Terrain terrain)
        {
            var map = new Tile[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    map[x, y] = new Tile { X = x, Y = y, Terrain = terrain };
                }
            }

            return map;
        }

        private static Terrain MakeTerrain(string name, int movement, bool walk = false, bool float_ = false, bool fly = false)
        {
            return Terrain.Create(new TerrainInfo
            {
                ShortName = name,
                DisplayName = name,
                Movement = movement,
                AllowWalk = walk,
                AllowFloat = float_,
                AllowFlight = fly
            });
        }
    }
}
