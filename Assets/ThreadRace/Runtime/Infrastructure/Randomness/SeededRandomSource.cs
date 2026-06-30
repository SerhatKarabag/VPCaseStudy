using System;
using ThreadRace.Core.Random;

namespace ThreadRace.Infrastructure.Randomness
{
    public sealed class SeededRandomSource : IDeterministicRandomSource
    {
        private const uint Multiplier = 1664525u;
        private const uint Increment = 1013904223u;
        private const uint SeedScrambler = 0xA5A5A5A5u;

        private uint _state;
        private int _consumedCount;

        public SeededRandomSource(int seed)
        {
            Seed = seed;
            _state = CreateInitialState(seed);
        }

        public SeededRandomSource(DeterministicRandomState state)
        {
            if (state.ConsumedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(state), "Cannot restore a random source from a negative consumed count.");
            }

            Seed = state.Seed;
            _state = unchecked((uint)state.State);
            _consumedCount = state.ConsumedCount;
        }

        public int Seed { get; }

        public DeterministicRandomState CurrentState => new DeterministicRandomState(Seed, unchecked((int)_state), _consumedCount);

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

            var sample = NextUnitFloat();
            return minInclusive + (float)(sample * (maxInclusive - minInclusive));
        }

        private double NextUnitFloat()
        {
            _state = unchecked((_state * Multiplier) + Increment);
            _consumedCount++;
            return _state / ((double)uint.MaxValue + 1d);
        }

        private static uint CreateInitialState(int seed)
        {
            return unchecked(((uint)seed) ^ SeedScrambler);
        }
    }
}
