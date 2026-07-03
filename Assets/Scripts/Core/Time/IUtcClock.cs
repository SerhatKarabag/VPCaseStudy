using System;

namespace ThreadRace.Core.Time
{
    public interface IUtcClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
