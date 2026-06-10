using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using WismCompanion.State;
using WismCompanion.UI;

namespace WismCompanion.Tests
{
    /// <summary>
    /// Verifies that LogView correctly filters and exposes entries after SetEntries is called.
    /// These tests exercise the data path only (DisplayedCount / SourceCount). Visual rendering
    /// requires a live Unity panel and is covered by PlayMode or manual screenshot checks.
    /// </summary>
    public sealed class LogViewTests
    {
        private static CompanionLogEntry Entry(string category, string summary = "test") =>
            new(DateTime.UtcNow, "default", category, summary, "detail");

        private static LogView Build(LogViewMode mode = LogViewMode.Raw)
        {
            var logView = new LogView(new ListView(), null);
            logView.SetMode(mode);
            return logView;
        }

        // ── Raw mode ───────────────────────────────────────────────────────────

        [Test]
        public void RawMode_AllCategoriesAreDisplayed()
        {
            var view = Build(LogViewMode.Raw);
            view.SetEntries(new List<CompanionLogEntry>
            {
                Entry("Map"), Entry("Command"), Entry("Battle")
            });

            Assert.That(view.SourceCount,    Is.EqualTo(3));
            Assert.That(view.DisplayedCount, Is.EqualTo(3));
        }

        [Test]
        public void RawMode_EmptyInput_DisplayedCountIsZero()
        {
            var view = Build(LogViewMode.Raw);
            view.SetEntries(new List<CompanionLogEntry>());

            Assert.That(view.SourceCount,    Is.EqualTo(0));
            Assert.That(view.DisplayedCount, Is.EqualTo(0));
        }

        [Test]
        public void RawMode_LargeInput_AllEntriesDisplayed()
        {
            var entries = new List<CompanionLogEntry>();
            for (int i = 0; i < 421; i++)
                entries.Add(Entry("Map", $"snapshot {i}"));

            var view = Build(LogViewMode.Raw);
            view.SetEntries(entries);

            Assert.That(view.DisplayedCount, Is.EqualTo(421));
        }

        // ── Simple mode ────────────────────────────────────────────────────────

        [Test]
        public void SimpleMode_FiltersOutMapEvents()
        {
            var view = Build(LogViewMode.Simple);
            view.SetEntries(new List<CompanionLogEntry>
            {
                Entry("Map"), Entry("Command"), Entry("Battle"), Entry("Map")
            });

            Assert.That(view.SourceCount,    Is.EqualTo(4));
            Assert.That(view.DisplayedCount, Is.EqualTo(2));
        }

        [Test]
        public void SimpleMode_OnlyMapEvents_DisplayedIsEmpty()
        {
            var view = Build(LogViewMode.Simple);
            view.SetEntries(new List<CompanionLogEntry>
            {
                Entry("Map"), Entry("Map"), Entry("Map")
            });

            Assert.That(view.SourceCount,    Is.EqualTo(3));
            Assert.That(view.DisplayedCount, Is.EqualTo(0));
        }

        // ── Mode switch ────────────────────────────────────────────────────────

        [Test]
        public void SwitchingFromSimpleToRaw_ExposesAllEntries()
        {
            var view = Build(LogViewMode.Simple);
            var entries = new List<CompanionLogEntry>
            {
                Entry("Map"), Entry("Command"), Entry("Map")
            };
            view.SetEntries(entries);
            Assert.That(view.DisplayedCount, Is.EqualTo(1), "Simple should show only Command");

            view.SetMode(LogViewMode.Raw);
            Assert.That(view.DisplayedCount, Is.EqualTo(3), "Raw should show all three");
        }

        // ── Text filter ────────────────────────────────────────────────────────

        [Test]
        public void SetFilter_NarrowsDisplayedItems()
        {
            var view = Build(LogViewMode.Raw);
            view.SetEntries(new List<CompanionLogEntry>
            {
                Entry("Map",     "tile update north"),
                Entry("Command", "move north"),
                Entry("Battle",  "attack castle south")
            });
            view.SetFilter("north");

            Assert.That(view.DisplayedCount, Is.EqualTo(2));
        }

        [Test]
        public void SetFilter_ClearFilter_RestoresFullCount()
        {
            var view = Build(LogViewMode.Raw);
            view.SetEntries(new List<CompanionLogEntry>
            {
                Entry("Map",     "tile update"),
                Entry("Command", "march north"),
                Entry("Battle",  "attack castle")
            });
            view.SetFilter("north");
            Assert.That(view.DisplayedCount, Is.EqualTo(1));

            view.SetFilter(string.Empty);
            Assert.That(view.DisplayedCount, Is.EqualTo(3));
        }

        // ── State integration ──────────────────────────────────────────────────

        [Test]
        public void StateGetLog_FeedsCorrectlyIntoRawMode()
        {
            var state = new CompanionState();
            state.ApplyMap(CompanionStateTests.Map("ch", 3, 3));
            state.ApplyCommand(CompanionStateTests.Command("ch", "Move"));

            var log = state.GetLog("ch");

            var view = Build(LogViewMode.Raw);
            view.SetEntries(log);

            Assert.That(view.SourceCount,    Is.EqualTo(2));
            Assert.That(view.DisplayedCount, Is.EqualTo(2));
        }

        [Test]
        public void StateGetLog_FeedsCorrectlyIntoSimpleMode()
        {
            var state = new CompanionState();
            state.ApplyMap(CompanionStateTests.Map("ch", 3, 3));    // Map
            state.ApplyCommand(CompanionStateTests.Command("ch", "Move"));  // Command
            state.ApplyMap(CompanionStateTests.Map("ch", 3, 3));    // Map

            var log = state.GetLog("ch");

            var view = Build(LogViewMode.Simple);
            view.SetEntries(log);

            Assert.That(view.SourceCount,    Is.EqualTo(3), "All three should be in source");
            Assert.That(view.DisplayedCount, Is.EqualTo(1), "Only the Command makes it through Simple filter");
        }
    }
}
