using System;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Models;

namespace ThreadRace.Presentation.Signals
{
    public readonly struct RaceSnapshotChangedSignal
    {
        public RaceSnapshotChangedSignal(RaceSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public RaceSnapshot Snapshot { get; }
    }

    public readonly struct HostGameplayStartedSignal
    {
    }

    public readonly struct HostLevelChangedSignal
    {
        public HostLevelChangedSignal(int currentLevel)
        {
            CurrentLevel = currentLevel;
        }

        public int CurrentLevel { get; }
    }

    public readonly struct HostGameplayScreenChangedSignal
    {
        public HostGameplayScreenChangedSignal(PlaceholderLevelScreen screen)
        {
            Screen = screen;
        }

        public PlaceholderLevelScreen Screen { get; }
    }

    public readonly struct HostGameplayBackHomeClickedSignal
    {
    }

    public readonly struct HostGameplayCompletedSignal
    {
        public HostGameplayCompletedSignal(LevelResult result, bool shouldOpenRacePopup)
        {
            Result = result;
            ShouldOpenRacePopup = shouldOpenRacePopup;
        }

        public LevelResult Result { get; }

        public bool ShouldOpenRacePopup { get; }
    }
}
