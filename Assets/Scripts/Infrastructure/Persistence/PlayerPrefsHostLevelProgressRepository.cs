using System;
using ThreadRace.Core.Progress;
using UnityEngine;

namespace ThreadRace.Infrastructure.Persistence
{
    public sealed class PlayerPrefsHostLevelProgressRepository : IHostLevelProgressRepository
    {
        private const string CurrentLevelKey = "ThreadRace.HostLevel.Current";
        private const int MinimumLevel = 1;

        public int LoadCurrentLevel()
        {
            var currentLevel = PlayerPrefs.GetInt(CurrentLevelKey, MinimumLevel);
            return currentLevel < MinimumLevel ? MinimumLevel : currentLevel;
        }

        public void SaveCurrentLevel(int currentLevel)
        {
            if (currentLevel < MinimumLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel), "Host level must be at least 1.");
            }

            PlayerPrefs.SetInt(CurrentLevelKey, currentLevel);
            PlayerPrefs.Save();
        }
    }
}
