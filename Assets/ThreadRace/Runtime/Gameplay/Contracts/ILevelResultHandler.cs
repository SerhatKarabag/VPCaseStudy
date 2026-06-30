using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Contracts
{
    public interface ILevelResultHandler
    {
        bool ReportLevelResult(LevelResult result);
    }
}
