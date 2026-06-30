namespace ThreadRace.Core.Random
{
    public interface IDeterministicRandomSource
    {
        int Seed { get; }

        float Range(float minInclusive, float maxInclusive);
    }
}
