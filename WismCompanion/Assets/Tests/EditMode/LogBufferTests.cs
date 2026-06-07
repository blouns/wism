using System;
using NUnit.Framework;
using WismCompanion.State;

namespace WismCompanion.Tests
{
    public sealed class LogBufferTests
    {
        [Test]
        public void Add_ReturnsCountAndStoresNewestFirst()
        {
            var buffer = new LogBuffer();

            var firstCount = buffer.Add(Entry("alpha", "first"));
            var secondCount = buffer.Add(Entry("alpha", "second"));

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(secondCount, Is.EqualTo(2));
            Assert.That(buffer.GetEntries("alpha")[0].Summary, Is.EqualTo("second"));
            Assert.That(buffer.GetEntries("alpha")[1].Summary, Is.EqualTo("first"));
        }

        [Test]
        public void Add_KeepsChannelsSeparate()
        {
            var buffer = new LogBuffer();

            buffer.Add(Entry("alpha", "a"));
            buffer.Add(Entry("beta", "b"));

            Assert.That(buffer.GetCount("alpha"), Is.EqualTo(1));
            Assert.That(buffer.GetCount("beta"), Is.EqualTo(1));
            Assert.That(buffer.GetEntries("alpha")[0].Summary, Is.EqualTo("a"));
            Assert.That(buffer.GetEntries("beta")[0].Summary, Is.EqualTo("b"));
        }

        [Test]
        public void GetEntries_UsesCaseInsensitiveChannels()
        {
            var buffer = new LogBuffer();

            buffer.Add(Entry("Alpha", "case"));

            Assert.That(buffer.GetCount("alpha"), Is.EqualTo(1));
            Assert.That(buffer.GetEntries("ALPHA")[0].Summary, Is.EqualTo("case"));
        }

        [Test]
        public void GetEntries_ReturnsEmptyForBlankOrUnknownChannel()
        {
            var buffer = new LogBuffer();

            Assert.That(buffer.GetEntries(string.Empty), Is.Empty);
            Assert.That(buffer.GetEntries("missing"), Is.Empty);
            Assert.That(buffer.GetCount(" "), Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesOnlySelectedChannel()
        {
            var buffer = new LogBuffer();
            buffer.Add(Entry("alpha", "a"));
            buffer.Add(Entry("beta", "b"));

            buffer.Clear("alpha");

            Assert.That(buffer.GetEntries("alpha"), Is.Empty);
            Assert.That(buffer.GetEntries("beta"), Has.Count.EqualTo(1));
        }

        [Test]
        public void Add_TrimsEachChannelToMaxEntries()
        {
            var buffer = new LogBuffer();

            for (var i = 0; i < LogBuffer.MaxEntriesPerChannel + 7; i++)
            {
                buffer.Add(Entry("alpha", "entry-" + i));
            }

            Assert.That(buffer.GetCount("alpha"), Is.EqualTo(LogBuffer.MaxEntriesPerChannel));
            Assert.That(buffer.GetEntries("alpha")[0].Summary, Is.EqualTo("entry-506"));
            Assert.That(buffer.GetEntries("alpha")[^1].Summary, Is.EqualTo("entry-7"));
        }

        private static CompanionLogEntry Entry(string channel, string summary) =>
            new(DateTime.UtcNow, channel, "Test", summary, "detail");
    }
}
