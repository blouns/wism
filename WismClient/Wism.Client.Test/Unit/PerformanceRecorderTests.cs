using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Wism.Client.Diagnostics;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class PerformanceRecorderTests
{
    [TearDown] public void Cleanup() => PerformanceProbe.Current = null;

    [Test] public void DisabledScopeHasNoCallbacks()
    {
        using (PerformanceProbe.Measure("disabled")) { }
        Assert.That(PerformanceProbe.Current, Is.Null);
    }

    [Test] public void NestedScopesHaveNonAdditiveInclusiveTimes()
    {
        var samples = new List<PerformanceSample>();
        int begins = 0, ends = 0;
        PerformanceProbe.Current = new PerformanceProbe(samples.Add, _ => begins++, () => ends++);
        using (PerformanceProbe.Measure("outer"))
        using (PerformanceProbe.Measure("inner")) Thread.SpinWait(10000);
        Assert.That(samples.Count, Is.EqualTo(2));
        Assert.That(begins, Is.EqualTo(ends));
        Assert.That(samples[1].DurationMs - samples[1].ExclusiveMs, Is.EqualTo(samples[0].DurationMs).Within(.001));
    }

    [Test] public void ExceptionClosesScopeAndMarker()
    {
        var samples = new List<PerformanceSample>();
        PerformanceProbe.Current = new PerformanceProbe(samples.Add);
        Assert.Throws<InvalidOperationException>(() => { using (PerformanceProbe.Measure("failed")) throw new InvalidOperationException(); });
        Assert.That(samples.Count, Is.EqualTo(1));
    }

    [Test] public void RecorderDrainsOnShutdownAndPreservesUnavailableValues()
    {
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "recorder-" + Guid.NewGuid().ToString("N"));
        using (var recorder = new LocalPerformanceRecorder(directory, diskFloor: 0))
            recorder.Record(new PerformanceSample { Kind = "frame", Value = null });
        var line = JObject.Parse(File.ReadAllText(Path.Combine(directory, "samples.jsonl")));
        Assert.That(line["Value"].Type, Is.EqualTo(JTokenType.Null));
        var receipt = JObject.Parse(File.ReadAllText(Path.Combine(directory, "receipt.json")));
        Assert.That((int)receipt["Written"], Is.EqualTo(1));
        Assert.That((int)receipt["Dropped"], Is.Zero);
    }

    [Test] public void RecorderCapsBytesAndReportsDroppedRecords()
    {
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "recorder-" + Guid.NewGuid().ToString("N"));
        using (var recorder = new LocalPerformanceRecorder(directory, capacity: 2, byteLimit: 1024, diskFloor: 0))
            for (int i = 0; i < 10000; i++) recorder.Record(new PerformanceSample { Kind = "test", Detail = new string('x', 500) });
        Assert.That(new FileInfo(Path.Combine(directory, "samples.jsonl")).Length, Is.LessThanOrEqualTo(1024));
        Assert.That((long)JObject.Parse(File.ReadAllText(Path.Combine(directory, "receipt.json")))["Dropped"], Is.GreaterThan(0));
    }

    [Test] public void RecorderStopsAtDiskFloor()
    {
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "recorder-" + Guid.NewGuid().ToString("N"));
        using (var recorder = new LocalPerformanceRecorder(directory, diskFloor: long.MaxValue))
        {
            recorder.Record(new PerformanceSample { Kind = "test" });
            Assert.That(SpinWait.SpinUntil(() => recorder.StopReason != null, 2000), Is.True);
            Assert.That(recorder.StopReason, Is.EqualTo("disk-floor"));
        }
    }
}
