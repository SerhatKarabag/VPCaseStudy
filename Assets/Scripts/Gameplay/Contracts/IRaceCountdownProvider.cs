using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Contracts
{
    public interface IRaceCountdownProvider
    {
        RaceCountdownSnapshot CurrentCountdown { get; }
    }
}
