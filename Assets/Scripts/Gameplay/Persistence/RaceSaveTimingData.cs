using System;

namespace ThreadRace.Gameplay.Persistence
{
    public sealed class RaceSaveTimingData
    {
        public RaceSaveTimingData(
            bool hasStarted,
            long startUtcUnixSeconds,
            long endUtcUnixSeconds,
            long lastObservedUtcUnixSeconds)
        {
            HasStarted = hasStarted;
            StartUtcUnixSeconds = startUtcUnixSeconds;
            EndUtcUnixSeconds = endUtcUnixSeconds;
            LastObservedUtcUnixSeconds = lastObservedUtcUnixSeconds;
        }

        public bool HasStarted { get; }

        public long StartUtcUnixSeconds { get; }

        public long EndUtcUnixSeconds { get; }

        public long LastObservedUtcUnixSeconds { get; }

        public static RaceSaveTimingData NotStarted()
        {
            return new RaceSaveTimingData(false, 0L, 0L, 0L);
        }

        public static RaceSaveTimingData Started(DateTimeOffset startUtc, DateTimeOffset endUtc, DateTimeOffset lastObservedUtc)
        {
            return new RaceSaveTimingData(
                true,
                startUtc.ToUnixTimeSeconds(),
                endUtc.ToUnixTimeSeconds(),
                lastObservedUtc.ToUnixTimeSeconds());
        }
    }
}
