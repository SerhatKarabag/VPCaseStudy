namespace ThreadRace.Core.Progress
{
    public interface IHostLevelProgressRepository
    {
        int LoadCurrentLevel();

        void SaveCurrentLevel(int currentLevel);
    }
}
