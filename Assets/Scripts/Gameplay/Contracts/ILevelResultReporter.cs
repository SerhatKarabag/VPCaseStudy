using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Contracts
{
    public interface ILevelResultReporter
    {
        void Report(LevelResult result);
    }
}
