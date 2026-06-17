using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Test.AI
{
    /// <summary>
    ///     Workstream A3 / V0: the influence field exports to a public-safe DTO and survives the
    ///     same Newtonsoft round-trip the telemetry publisher uses, while staying back-compatible
    ///     with snapshots that carry no field.
    /// </summary>
    [TestFixture]
    public class InfluenceFieldExporterTests
    {
        [Test]
        public void Export_SamplesEveryCell_RowMajor()
        {
            var advisor = new DeterministicAdvisor();

            var dto = InfluenceFieldExporter.Export(advisor, width: 4, height: 3);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Width, Is.EqualTo(4));
            Assert.That(dto.Height, Is.EqualTo(3));
            Assert.That(dto.Tension.Length, Is.EqualTo(12));

            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var i = dto.IndexOf(x, y);
                    Assert.That(dto.Tension[i], Is.EqualTo((float)advisor.GetTension(x, y)), $"tension({x},{y})");
                    Assert.That(dto.Friendly[i], Is.EqualTo((float)advisor.GetFriendly(x, y)), $"friendly({x},{y})");
                    Assert.That(dto.Enemy[i], Is.EqualTo((float)advisor.GetEnemy(x, y)), $"enemy({x},{y})");
                }
            }
        }

        [Test]
        public void Export_ReturnsNull_ForMissingAdvisorOrEmptyGrid()
        {
            Assert.That(InfluenceFieldExporter.Export(null, 4, 4), Is.Null);
            Assert.That(InfluenceFieldExporter.Export(new DeterministicAdvisor(), 0, 4), Is.Null);
            Assert.That(InfluenceFieldExporter.Export(new DeterministicAdvisor(), 4, -1), Is.Null);
        }

        [Test]
        public void MapSnapshot_RoundTripsInfluenceField_ThroughPublisherJson()
        {
            var snapshot = new MapSnapshot
            {
                Width = 2,
                Height = 2,
                Influence = new InfluenceFieldDto
                {
                    Width = 2,
                    Height = 2,
                    Tension = new[] { -1f, -0.25f, 0.25f, 1f },
                    Friendly = new[] { 0f, 0.1f, 0.5f, 1f },
                    Enemy = new[] { 1f, 0.35f, 0.25f, 0f }
                }
            };

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var json = JsonConvert.SerializeObject(snapshot, settings);
            var restored = JsonConvert.DeserializeObject<MapSnapshot>(json, settings);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Influence, Is.Not.Null);
            Assert.That(restored.Influence!.Width, Is.EqualTo(2));
            Assert.That(restored.Influence.Tension, Is.EqualTo(snapshot.Influence.Tension));
            Assert.That(restored.Influence.Friendly, Is.EqualTo(snapshot.Influence.Friendly));
            Assert.That(restored.Influence.Enemy, Is.EqualTo(snapshot.Influence.Enemy));
        }

        [Test]
        public void MapSnapshot_WithoutInfluence_StaysBackCompatible()
        {
            var restored = JsonConvert.DeserializeObject<MapSnapshot>(
                """{"Width":3,"Height":2,"Tiles":[],"Armies":[],"Cities":[]}""");

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Influence, Is.Null);
        }

        /// <summary>An advisor with closed-form values, so the export is checked without a flood.</summary>
        private sealed class DeterministicAdvisor : ISpatialAdvisor
        {
            public double GetTension(int x, int y) => (x * 0.1) - (y * 0.2);

            public double GetFriendly(int x, int y) => x * 0.25;

            public double GetEnemy(int x, int y) => y * 0.5;

            public double GetTension(Tile tile) => GetTension(tile.X, tile.Y);

            public double GetFriendly(Tile tile) => GetFriendly(tile.X, tile.Y);

            public double GetEnemy(Tile tile) => GetEnemy(tile.X, tile.Y);

            public double GetRawFriendly(Tile tile) => GetFriendly(tile.X, tile.Y);

            public double GetRawEnemy(Tile tile) => GetEnemy(tile.X, tile.Y);

            public bool IsFrontLine(Tile tile) => false;

            public Tile GetGradientStep(Tile from, bool ascendFriendly) => from;

            public double GetInfluence(Tile tile) => GetEnemy(tile);

            public void Update()
            {
            }
        }
    }
}
