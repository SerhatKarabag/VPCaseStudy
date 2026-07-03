using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Presentation.Signals
{
    public readonly struct RaceCountdownChangedSignal
    {
        public RaceCountdownChangedSignal(RaceCountdownSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public RaceCountdownSnapshot Snapshot { get; }
    }
}
