using System;
using ThreadRace.Core.Time;

namespace ThreadRace.Infrastructure.Time
{
    public sealed class SystemUtcClock : IUtcClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
