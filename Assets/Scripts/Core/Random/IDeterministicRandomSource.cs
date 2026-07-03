namespace ThreadRace.Core.Random
{
    public interface IDeterministicRandomSource
    {
        int Seed { get; }

        DeterministicRandomState CurrentState { get; }

        float Range(float minInclusive, float maxInclusive);
    }
}
