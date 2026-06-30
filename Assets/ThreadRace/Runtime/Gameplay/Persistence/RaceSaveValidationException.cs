using System;

namespace ThreadRace.Gameplay.Persistence
{
    public sealed class RaceSaveValidationException : Exception
    {
        public RaceSaveValidationException(string message)
            : base(message)
        {
        }
    }
}
