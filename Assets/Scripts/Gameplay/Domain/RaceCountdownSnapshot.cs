using System;

namespace ThreadRace.Gameplay.Domain
{
    public sealed class RaceCountdownSnapshot
    {
        public RaceCountdownSnapshot(bool isActive, long remainingSeconds, bool isExpired, DateTimeOffset? eventEndUtc)
        {
            if (remainingSeconds < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingSeconds), "Remaining seconds must not be negative.");
            }

            if (!isActive && remainingSeconds != 0L)
            {
                throw new ArgumentException("Inactive countdowns must not expose remaining time.", nameof(remainingSeconds));
            }

            IsActive = isActive;
            RemainingSeconds = remainingSeconds;
            IsExpired = isExpired;
            EventEndUtc = eventEndUtc;
        }

        public bool IsActive { get; }

        public long RemainingSeconds { get; }

        public bool IsExpired { get; }

        public DateTimeOffset? EventEndUtc { get; }
    }
}
