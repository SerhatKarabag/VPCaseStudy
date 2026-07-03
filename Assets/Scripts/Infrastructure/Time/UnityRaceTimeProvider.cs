using ThreadRace.Core.Time;

namespace ThreadRace.Infrastructure.Time
{
    public sealed class UnityRaceTimeProvider : IRaceTimeProvider
    {
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;
    }
}
