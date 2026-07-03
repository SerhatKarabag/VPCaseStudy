using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Contracts
{
    public interface IRaceSnapshotProvider
    {
        RaceSnapshot CurrentSnapshot { get; }
    }
}
