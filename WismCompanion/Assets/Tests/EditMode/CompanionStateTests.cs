using System;
using NUnit.Framework;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;
using WismCompanion.State;

namespace WismCompanion.Tests
{
    public sealed class CompanionStateTests
    {
        [Test]
        public void ApplyMap_TracksFirstChannelAndStoresLatestMap()
        {
            var state = new CompanionState();
            var map = Map("playground:a", 3, 4);

            state.ApplyMap(map);

            Assert.That(state.SelectedChannel, Is.EqualTo("playground:a"));
            Assert.That(state.Channels, Is.EqualTo(new[] { "playground:a" }));
            Assert.That(state.GetLatestMap("playground:a"), Is.SameAs(map));
            Assert.That(state.TotalEventsReceived, Is.EqualTo(1));
        }

        [Test]
        public void ApplyMap_RaisesChannelAndChangeEventsForActiveChannel()
        {
            var state = new CompanionState();
            var changed = 0;
            var channelsChanged = 0;
            state.Changed += () => changed++;
            state.ChannelsChanged += () => channelsChanged++;

            state.ApplyMap(Map("alpha", 3, 4));

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(channelsChanged, Is.EqualTo(1));
        }

        [Test]
        public void ApplyCommand_AddsLogAndTracksChannel()
        {
            var state = new CompanionState();

            state.ApplyCommand(Command("beta", "Move"));

            Assert.That(state.SelectedChannel, Is.EqualTo("beta"));
            Assert.That(state.GetLog("beta"), Has.Count.EqualTo(1));
            Assert.That(state.GetLog("beta")[0].Summary, Does.Contain("Move"));
            Assert.That(state.TotalEventsReceived, Is.EqualTo(1));
        }

        [Test]
        public void SelectChannel_RaisesChangedAndSwitchesActiveLog()
        {
            var state = new CompanionState();
            state.ApplyMap(Map("alpha", 2, 2));
            state.ApplyCommand(Command("beta", "Attack"));
            var changed = 0;
            state.Changed += () => changed++;

            state.SelectChannel("beta");

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(state.SelectedChannel, Is.EqualTo("beta"));
            Assert.That(state.GetLog(state.SelectedChannel)[0].Category, Is.EqualTo("Command"));
        }

        [Test]
        public void SelectChannel_IgnoresBlankAndSameChannel()
        {
            var state = new CompanionState();
            state.ApplyMap(Map("alpha", 2, 2));
            var changed = 0;
            state.Changed += () => changed++;

            state.SelectChannel(" ");
            state.SelectChannel("ALPHA");

            Assert.That(changed, Is.EqualTo(0));
            Assert.That(state.SelectedChannel, Is.EqualTo("alpha"));
        }

        [Test]
        public void ApplyMap_UpdatesLatestMapWithoutDuplicatingChannel()
        {
            var state = new CompanionState();
            state.ApplyMap(Map("alpha", 2, 2));

            state.ApplyMap(Map("ALPHA", 8, 9));

            Assert.That(state.Channels, Has.Count.EqualTo(1));
            Assert.That(state.GetLatestMap("alpha").Width, Is.EqualTo(8));
            Assert.That(state.TotalEventsReceived, Is.EqualTo(2));
        }

        [Test]
        public void NullEventsAreIgnored()
        {
            var state = new CompanionState();

            state.ApplyMap(null);
            state.ApplyCommand(null);

            Assert.That(state.TotalEventsReceived, Is.EqualTo(0));
            Assert.That(state.Channels, Is.Empty);
            Assert.That(state.GetLatestMap("alpha"), Is.Null);
        }

        internal static MapSnapshot Map(string channel, int width, int height) =>
            new()
            {
                Width = width,
                Height = height,
                Timestamp = new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc),
                Telemetry = new TelemetryContext { ChannelId = channel },
                Tiles = { new TileDto { X = 1, Y = 2, TerrainType = "Grass", HasCity = true } },
                Cities = { new CityDto { Name = "Marthos", Owner = "Sirians", Defense = 2, Position = new PositionDto { X = 1, Y = 2 } } },
                Armies = { new ArmyDto { Name = "Hero", Owner = "Sirians", Health = 3, IsHero = true, Position = new PositionDto { X = 1, Y = 2 } } },
                Locations = { new LocationDto { Name = "Old Ruins", Type = "Ruins", Position = new PositionDto { X = 2, Y = 2 } } },
                Items = { new ItemDto { Name = "Staff", Position = new PositionDto { X = 1, Y = 2 } } }
            };

        internal static CommandExecutedEvent Command(string channel, string type) =>
            new()
            {
                CommandType = type,
                ActorId = "Hero",
                TargetPosition = new PositionDto { X = 4, Y = 5 },
                Parameters = { ["direction"] = "north" },
                Result = "Succeeded",
                Timestamp = new DateTime(2026, 6, 6, 12, 1, 0, DateTimeKind.Utc),
                Telemetry = new TelemetryContext { ChannelId = channel }
            };
    }
}
