using System;

namespace Wism.Client.Data
{
    /// <summary>
    ///     Seed-compatible random source with explicit, persistable state.
    /// </summary>
    public sealed class DeterministicRandom : Random
    {
        private const int Big = int.MaxValue;
        private const int SeedArrayLength = 56;
        private const int MagicSeed = 161803398;

        private readonly int[] seedArray;
        private int inext;
        private int inextp;

        public DeterministicRandom(int seed)
        {
            seedArray = Initialize(seed);
            inext = 0;
            inextp = 21;
        }

        public DeterministicRandom(int[] seedArray, int inext, int inextp)
        {
            if (seedArray == null || seedArray.Length != SeedArrayLength)
            {
                throw new ArgumentException(
                    $"Random state must contain {SeedArrayLength} values.",
                    nameof(seedArray));
            }

            if (inext < 0 || inext >= SeedArrayLength)
            {
                throw new ArgumentOutOfRangeException(nameof(inext));
            }

            if (inextp < 0 || inextp >= SeedArrayLength)
            {
                throw new ArgumentOutOfRangeException(nameof(inextp));
            }

            this.seedArray = (int[])seedArray.Clone();
            this.inext = inext;
            this.inextp = inextp;
        }

        public int Inext => inext;

        public int Inextp => inextp;

        public int[] ExportSeedArray()
        {
            return (int[])seedArray.Clone();
        }

        public override int Next()
        {
            return InternalSample();
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            return (int)(Sample() * maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue));
            }

            var range = (long)maxValue - minValue;
            return range <= int.MaxValue
                ? (int)(Sample() * range) + minValue
                : (int)((long)(GetSampleForLargeRange() * range) + minValue);
        }

        public override double NextDouble()
        {
            return Sample();
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(InternalSample() % 256);
            }
        }

        protected override double Sample()
        {
            return InternalSample() * (1.0 / Big);
        }

        private static int[] Initialize(int seed)
        {
            var state = new int[SeedArrayLength];
            var subtraction = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
            var mj = MagicSeed - subtraction;
            state[55] = mj;
            var mk = 1;

            for (var i = 1; i < 55; i++)
            {
                var ii = 21 * i % 55;
                state[ii] = mk;
                mk = mj - mk;
                if (mk < 0)
                {
                    mk += Big;
                }

                mj = state[ii];
            }

            for (var k = 1; k < 5; k++)
            {
                for (var i = 1; i < 56; i++)
                {
                    state[i] -= state[1 + (i + 30) % 55];
                    if (state[i] < 0)
                    {
                        state[i] += Big;
                    }
                }
            }

            return state;
        }

        private int InternalSample()
        {
            var next = inext + 1;
            if (next >= SeedArrayLength)
            {
                next = 1;
            }

            var nextp = inextp + 1;
            if (nextp >= SeedArrayLength)
            {
                nextp = 1;
            }

            var value = seedArray[next] - seedArray[nextp];
            if (value == Big)
            {
                value--;
            }

            if (value < 0)
            {
                value += Big;
            }

            seedArray[next] = value;
            inext = next;
            inextp = nextp;
            return value;
        }

        private double GetSampleForLargeRange()
        {
            var result = InternalSample();
            if (InternalSample() % 2 == 0)
            {
                result = -result;
            }

            return (result + (double)int.MaxValue - 1) / (2 * (uint)int.MaxValue - 1);
        }
    }
}
