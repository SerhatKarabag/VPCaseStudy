namespace ThreadRace.Core.Random
{
    public interface IDeterministicRandomSourceFactory
    {
        IDeterministicRandomSource Create(int seed);

        IDeterministicRandomSource Restore(DeterministicRandomState state);
    }
}
