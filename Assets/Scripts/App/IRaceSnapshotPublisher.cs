using ThreadRace.Gameplay.Domain;

namespace ThreadRace.App
{
    public interface IRaceSnapshotPublisher
    {
        void Publish(RaceSnapshot snapshot);
    }
}
