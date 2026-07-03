using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Contracts
{
    public interface IRaceEventCommandHandler
    {
        bool StartRace();

        bool ReportLevelResult(LevelResult result);

        bool ResolveExpiredEvent();

        bool ClaimReward();

        void ResetRace();
    }
}
