namespace ThreadRace.Core.Progress
{
    public interface IHostLevelProgressService
    {
        int CurrentLevel { get; }

        int LoadCurrentLevel();

        int AdvanceAfterSuccess();
    }
}
