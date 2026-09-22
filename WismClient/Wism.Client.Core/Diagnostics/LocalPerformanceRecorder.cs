using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Wism.Client.Diagnostics
{
    // Fixed-capacity queue. Serialization and file I/O never run on the producer thread.
    public sealed class LocalPerformanceRecorder : IDisposable
    {
        private readonly PerformanceSample[] queue;
        private readonly object gate = new object();
        private readonly Thread writer;
        private readonly string directory;
        private readonly long byteLimit, diskFloor;
        private readonly TimeSpan duration;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private int head, tail, count;
        private bool closing;
        private long dropped, written, bytes;
        private volatile string stopReason;
        public string StopReason => stopReason;
        public long Dropped => Interlocked.Read(ref dropped);
        public bool IsRecording => !closing && stopReason == null;

        public LocalPerformanceRecorder(string directory, int capacity = 8192,
            long byteLimit = 256L * 1024 * 1024, long diskFloor = 80L * 1024 * 1024 * 1024,
            TimeSpan? duration = null)
        {
            if (capacity < 1 || byteLimit < 1024 || diskFloor < 0) throw new ArgumentOutOfRangeException();
            this.directory = Path.GetFullPath(directory);
            if (Directory.Exists(this.directory)) throw new IOException("Use a new diagnostic session directory.");
            Directory.CreateDirectory(this.directory);
            queue = new PerformanceSample[capacity];
            this.byteLimit = byteLimit;
            this.diskFloor = diskFloor;
            this.duration = duration ?? TimeSpan.FromHours(2);
            writer = new Thread(WriteLoop) { IsBackground = true, Name = "WISM diagnostics output" };
            writer.Start();
        }

        public void Record(PerformanceSample sample)
        {
            lock (gate)
            {
                if (closing || stopReason != null || count == queue.Length) { dropped++; return; }
                // Strings are immutable; reject unexpectedly large payloads rather than retain them.
                if ((sample.Detail?.Length ?? 0) > 8192) { dropped++; return; }
                if (sample.Timestamp == 0) sample.Timestamp = Stopwatch.GetTimestamp();
                queue[tail] = sample;
                tail = (tail + 1) % queue.Length;
                count++;
                Monitor.Pulse(gate);
            }
        }

        private void WriteLoop()
        {
            try
            {
                using (var output = new FileStream(Path.Combine(directory, "samples.jsonl"), FileMode.CreateNew,
                    FileAccess.Write, FileShare.Read, 65536))
                {
                    var disk = new DriveInfo(Path.GetPathRoot(directory));
                    long nextCheck = 0;
                    while (true)
                    {
                        PerformanceSample sample;
                        lock (gate)
                        {
                            if (count == 0 && !closing) Monitor.Wait(gate, 250);
                            if (count == 0 && closing) break;
                            if (count == 0) { sample = default; }
                            else { sample = queue[head]; queue[head] = default; head = (head + 1) % queue.Length; count--; }
                        }
                        if (clock.ElapsedMilliseconds >= nextCheck)
                        {
                            output.Flush();
                            nextCheck = clock.ElapsedMilliseconds + 1000;
                            if (disk.AvailableFreeSpace < diskFloor) { stopReason = "disk-floor"; if (sample.Kind != null) Interlocked.Increment(ref dropped); break; }
                            if (clock.Elapsed >= duration) { stopReason = "duration-limit"; if (sample.Kind != null) Interlocked.Increment(ref dropped); break; }
                        }
                        if (sample.Kind == null) continue;
                        var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sample) + "\n");
                        if (bytes + data.Length > byteLimit) { stopReason = "byte-limit"; Interlocked.Increment(ref dropped); break; }
                        output.Write(data, 0, data.Length);
                        bytes += data.Length;
                        written++;
                    }
                }
            }
            catch (Exception ex) { stopReason = "output-error:" + ex.GetType().Name; }
            finally
            {
                lock (gate) { dropped += count; count = 0; Array.Clear(queue, 0, queue.Length); }
                stopReason = stopReason ?? "closed";
                try
                {
                    File.WriteAllText(Path.Combine(directory, "receipt.json"), JsonConvert.SerializeObject(new {
                        Status = stopReason, Written = written, Dropped, Bytes = bytes, ByteLimit = byteLimit,
                        StopwatchFrequency = Stopwatch.Frequency, ClosedAtUtc = DateTime.UtcNow,
                        RawExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                    }, Formatting.Indented));
                }
                catch (IOException) { /* No recovery writes if the output volume is unavailable. */ }
                catch (UnauthorizedAccessException) { }
            }
        }

        public void Dispose()
        {
            lock (gate) { closing = true; Monitor.PulseAll(gate); }
            // A slow/unavailable volume must not freeze the game during shutdown.
            writer.Join(2000);
        }
    }
}
