namespace ThreadRace.Core.Time
{
    public interface IRaceTimeProvider
    {
        float UnscaledDeltaTime { get; }
    }
}
