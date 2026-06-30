using ThreadRace.Core.Random;

namespace ThreadRace.Infrastructure.Randomness
{
    public sealed class SeededRandomSourceFactory : IDeterministicRandomSourceFactory
    {
        public IDeterministicRandomSource Create(int seed)
        {
            return new SeededRandomSource(seed);
        }

        public IDeterministicRandomSource Restore(DeterministicRandomState state)
        {
            return new SeededRandomSource(state);
        }
    }
}
