using System;
using NUnit.Framework;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;
using WismCompanion.State;

namespace WismCompanion.Tests
{
    public sealed class ReplayBufferTests
    {
        [Test]
        public void RecordsMapsAndAssociatesEndTurnWithNextFrame()
        {
            var state = new CompanionState();

            state.ApplyMap(Map("alpha", 1, 1));
            state.ApplyCommand(Command("alpha", "EndTurnCommand"));
            state.ApplyMap(Map("alpha", 2, 2));

            var turns = state.GetReplayTurns("alpha");

            Assert.That(turns, Has.Count.EqualTo(2));
            Assert.That(turns[0].IsComplete, Is.True);
            Assert.That(turns[1].Frames[0].IsTurnBoundary, Is.True);
            Assert.That(turns[1].Frames[0].TriggerCommand.CommandType, Is.EqualTo("EndTurnCommand"));
        }

        [Test]
        public void RetainsCurrentTurnPlusTwoCompletedTurns()
        {
            var state = new CompanionState();

            state.ApplyMap(Map("alpha", 1, 1));
            EndTurnThenMap(state, "alpha", 2);
            EndTurnThenMap(state, "alpha", 3);
            EndTurnThenMap(state, "alpha", 4);

            var turns = state.GetReplayTurns("alpha");

            Assert.That(turns, Has.Count.EqualTo(3));
            Assert.That(turns[0].TurnKey, Is.EqualTo("turn-0002"));
            Assert.That(turns[1].TurnKey, Is.EqualTo("turn-0003"));
            Assert.That(turns[2].TurnKey, Is.EqualTo("turn-0004"));
        }

        [Test]
        public void ReplayCursorCanMoveByTurnAndReturnLive()
        {
            var state = new CompanionState();
            state.ApplyMap(Map("alpha", 1, 1));
            EndTurnThenMap(state, "alpha", 2);
            EndTurnThenMap(state, "alpha", 3);

            Assert.That(state.EnterReplay(), Is.True);
            Assert.That(state.GetVisibleMap("alpha").Width, Is.EqualTo(3));

            state.PreviousReplayTurn();
            Assert.That(state.ReplayMode, Is.EqualTo(ReplayViewMode.Replay));
            Assert.That(state.GetVisibleMap("alpha").Width, Is.EqualTo(2));

            state.GoLive();
            Assert.That(state.ReplayMode, Is.EqualTo(ReplayViewMode.Live));
            Assert.That(state.GetVisibleMap("alpha").Width, Is.EqualTo(3));
        }

        [Test]
        public void ReplayModeDoesNotDropIncomingLiveFrames()
        {
            var state = new CompanionState();
            state.ApplyMap(Map("alpha", 1, 1));
            EndTurnThenMap(state, "alpha", 2);
            state.EnterReplay();
            state.PreviousReplayTurn();

            state.ApplyMap(Map("alpha", 9, 9));

            Assert.That(state.ReplayMode, Is.EqualTo(ReplayViewMode.Replay));
            Assert.That(state.GetLatestMap("alpha").Width, Is.EqualTo(9));
            Assert.That(state.GetVisibleMap("alpha").Width, Is.EqualTo(1));

            state.GoLive();
            Assert.That(state.GetVisibleMap("alpha").Width, Is.EqualTo(9));
        }

        [Test]
        public void ExportReplayJsonIncludesVersionChannelFramesAndCommand()
        {
            var state = new CompanionState();
            state.ApplyMap(Map("alpha", 1, 1));
            state.ApplyCommand(Command("alpha", "EndTurnCommand"));
            state.ApplyMap(Map("alpha", 2, 2));

            var json = state.ExportReplayJson("alpha");

            Assert.That(json, Does.Contain("\"FormatVersion\": 1"));
            Assert.That(json, Does.Contain("\"Channel\": \"alpha\""));
            Assert.That(json, Does.Contain("\"Frames\""));
            Assert.That(json, Does.Contain("\"CommandType\": \"EndTurnCommand\""));
            Assert.That(json, Does.Contain("\"Snapshot\""));
        }

        private static void EndTurnThenMap(CompanionState state, string channel, int size)
        {
            state.ApplyCommand(Command(channel, "EndTurnCommand"));
            state.ApplyMap(Map(channel, size, size));
        }

        private static MapSnapshot Map(string channel, int width, int height) =>
            new()
            {
                Width = width,
                Height = height,
                Timestamp = new DateTime(2026, 6, 6, 12, width, 0, DateTimeKind.Utc),
                Telemetry = new TelemetryContext { ChannelId = channel },
                Tiles = { new TileDto { X = 1, Y = 2, TerrainType = "Grass" } }
            };

        private static CommandExecutedEvent Command(string channel, string type) =>
            new()
            {
                CommandType = type,
                ActorId = "Hero",
                TargetPosition = new PositionDto { X = 4, Y = 5 },
                Parameters = { ["direction"] = "north" },
                Result = "Succeeded",
                Timestamp = new DateTime(2026, 6, 6, 12, 30, 0, DateTimeKind.Utc),
                Telemetry = new TelemetryContext { ChannelId = channel }
            };
    }
}
