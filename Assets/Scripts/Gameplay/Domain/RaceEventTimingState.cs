using System;

namespace ThreadRace.Gameplay.Domain
{
    public sealed class RaceEventTimingState
    {
        private RaceEventTimingState(
            bool hasStarted,
            DateTimeOffset? startUtc,
            DateTimeOffset? endUtc,
            DateTimeOffset? lastObservedUtc)
        {
            HasStarted = hasStarted;
            StartUtc = startUtc;
            EndUtc = endUtc;
            LastObservedUtc = lastObservedUtc;
        }

        public bool HasStarted { get; }

        public DateTimeOffset? StartUtc { get; }

        public DateTimeOffset? EndUtc { get; }

        public DateTimeOffset? LastObservedUtc { get; }

        public static RaceEventTimingState NotStarted()
        {
            return new RaceEventTimingState(false, null, null, null);
        }

        public static RaceEventTimingState Started(
            DateTimeOffset startUtc,
            DateTimeOffset endUtc,
            DateTimeOffset lastObservedUtc)
        {
            if (endUtc <= startUtc)
            {
                throw new ArgumentException("Event end UTC must be after start UTC.", nameof(endUtc));
            }

            if (lastObservedUtc < startUtc)
            {
                throw new ArgumentException("Last observed UTC must not be before start UTC.", nameof(lastObservedUtc));
            }

            return new RaceEventTimingState(true, startUtc, endUtc, lastObservedUtc);
        }

        public RaceEventTimingState WithLastObservedUtc(DateTimeOffset observedUtc)
        {
            if (!HasStarted)
            {
                return this;
            }

            var startUtc = StartUtc.Value;
            var currentLastObservedUtc = LastObservedUtc.Value;
            var effectiveObservedUtc = observedUtc < currentLastObservedUtc ? currentLastObservedUtc : observedUtc;
            if (effectiveObservedUtc < startUtc)
            {
                effectiveObservedUtc = startUtc;
            }

            return Started(startUtc, EndUtc.Value, effectiveObservedUtc);
        }

        public DateTimeOffset GetEffectiveUtc(DateTimeOffset currentUtc)
        {
            if (!HasStarted)
            {
                return currentUtc;
            }

            return currentUtc < LastObservedUtc.Value ? LastObservedUtc.Value : currentUtc;
        }

        public long GetRemainingSeconds(DateTimeOffset effectiveUtc)
        {
            if (!HasStarted)
            {
                return 0L;
            }

            var remaining = EndUtc.Value - effectiveUtc;
            if (remaining <= TimeSpan.Zero)
            {
                return 0L;
            }

            return (long)Math.Ceiling(remaining.TotalSeconds);
        }

        public long GetElapsedSecondsTo(DateTimeOffset boundaryUtc)
        {
            if (!HasStarted)
            {
                return 0L;
            }

            if (boundaryUtc <= LastObservedUtc.Value)
            {
                return 0L;
            }

            return (long)Math.Floor((boundaryUtc - LastObservedUtc.Value).TotalSeconds);
        }
    }
}
