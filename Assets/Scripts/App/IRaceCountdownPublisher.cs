using ThreadRace.Gameplay.Domain;

namespace ThreadRace.App
{
    public interface IRaceCountdownPublisher
    {
        void Publish(RaceCountdownSnapshot snapshot);
    }
}
