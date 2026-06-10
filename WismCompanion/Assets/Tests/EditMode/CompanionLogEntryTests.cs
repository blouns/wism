using System;
using NUnit.Framework;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;
using WismCompanion.State;

namespace WismCompanion.Tests
{
    public sealed class CompanionLogEntryTests
    {
        [Test]
        public void FromCommand_FormatsActorTargetParametersAndResult()
        {
            var entry = CompanionLogEntry.FromCommand(CompanionStateTests.Command("alpha", "Move"));

            Assert.That(entry.ChannelId, Is.EqualTo("alpha"));
            Assert.That(entry.Category, Is.EqualTo("Command"));
            Assert.That(entry.Summary, Does.Contain("Move"));
            Assert.That(entry.Summary, Does.Contain("Hero"));
            Assert.That(entry.Summary, Does.Contain("(4,5)"));
            Assert.That(entry.Detail, Does.Contain("direction=north"));
            Assert.That(entry.Result, Is.EqualTo("Succeeded"));
        }

        [Test]
        public void FromCommand_UsesFallbacksForMissingActorTargetAndParameters()
        {
            var command = new CommandExecutedEvent
            {
                CommandType = "EndTurn",
                Timestamp = new DateTime(2026, 6, 6, 12, 2, 0, DateTimeKind.Utc)
            };

            var entry = CompanionLogEntry.FromCommand(command);

            Assert.That(entry.ChannelId, Is.EqualTo(TelemetryContext.DefaultChannelId));
            Assert.That(entry.Summary, Does.Contain("?"));
            Assert.That(entry.Detail, Does.Contain("none"));
        }

        [Test]
        public void FromMap_FormatsSizeAndEntityCounts()
        {
            var entry = CompanionLogEntry.FromMap(CompanionStateTests.Map("alpha", 9, 7));

            Assert.That(entry.ChannelId, Is.EqualTo("alpha"));
            Assert.That(entry.Category, Is.EqualTo("Map"));
            Assert.That(entry.Summary, Does.Contain("9x7"));
            Assert.That(entry.Summary, Does.Contain("1 armies"));
            Assert.That(entry.Summary, Does.Contain("1 cities"));
            Assert.That(entry.Detail, Does.Contain("Locations: 1"));
            Assert.That(entry.Detail, Does.Contain("Items: 1"));
        }

        [Test]
        public void LocalTime_IsFormattedForDisplay()
        {
            var entry = new CompanionLogEntry(
                new DateTime(2026, 6, 6, 12, 3, 4, DateTimeKind.Utc),
                "alpha",
                "Test",
                "summary",
                "detail");

            Assert.That(entry.LocalTime, Does.Match(@"^\d{2}:\d{2}:\d{2}$"));
        }
    }
}
