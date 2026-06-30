using System;
using ThreadRace.Core.Random;

namespace ThreadRace.Infrastructure.Randomness
{
    public sealed class SeededRandomSource : IDeterministicRandomSource
    {
        private readonly System.Random _random;

        public SeededRandomSource(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        public int Seed { get; }

        public float Range(float minInclusive, float maxInclusive)
        {
            if (float.IsNaN(minInclusive) || float.IsNaN(maxInclusive))
            {
                throw new ArgumentException("Random range values must not be NaN.");
            }

            if (float.IsInfinity(minInclusive) || float.IsInfinity(maxInclusive))
            {
                throw new ArgumentException("Random range values must be finite.");
            }

            if (minInclusive > maxInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(minInclusive), "Minimum random range value must be less than or equal to the maximum.");
            }

            if (minInclusive == maxInclusive)
            {
                return minInclusive;
            }

            var sample = _random.NextDouble();
            return minInclusive + (float)(sample * (maxInclusive - minInclusive));
        }
    }
}
