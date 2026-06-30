using System;

namespace ThreadRace.Core.Random
{
    public readonly struct DeterministicRandomState : IEquatable<DeterministicRandomState>
    {
        public DeterministicRandomState(int seed, int state, int consumedCount)
        {
            if (consumedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(consumedCount), "Consumed random value count must not be negative.");
            }

            Seed = seed;
            State = state;
            ConsumedCount = consumedCount;
        }

        public int Seed { get; }

        public int State { get; }

        public int ConsumedCount { get; }

        public bool Equals(DeterministicRandomState other)
        {
            return Seed == other.Seed
                && State == other.State
                && ConsumedCount == other.ConsumedCount;
        }

        public override bool Equals(object obj)
        {
            return obj is DeterministicRandomState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Seed;
                hash = (hash * 397) ^ State;
                hash = (hash * 397) ^ ConsumedCount;
                return hash;
            }
        }

        public static bool operator ==(DeterministicRandomState left, DeterministicRandomState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DeterministicRandomState left, DeterministicRandomState right)
        {
            return !left.Equals(right);
        }
    }
}
