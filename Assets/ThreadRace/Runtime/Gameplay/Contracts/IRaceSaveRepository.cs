using ThreadRace.Gameplay.Persistence;

namespace ThreadRace.Gameplay.Contracts
{
    public interface IRaceSaveRepository
    {
        RaceSaveLoadResult Load(string saveKey);

        void Save(string saveKey, RaceSaveData saveData);

        void Clear(string saveKey);
    }
}
