using System;

namespace ThreadRace.Gameplay.Persistence
{
    public sealed class RaceSaveLoadResult
    {
        private RaceSaveLoadResult(RaceSaveLoadStatus status, RaceSaveData saveData, string errorMessage)
        {
            Status = status;
            SaveData = saveData;
            ErrorMessage = errorMessage;
        }

        public RaceSaveLoadStatus Status { get; }

        public RaceSaveData SaveData { get; }

        public string ErrorMessage { get; }

        public static RaceSaveLoadResult NotFound()
        {
            return new RaceSaveLoadResult(RaceSaveLoadStatus.NotFound, null, null);
        }

        public static RaceSaveLoadResult Loaded(RaceSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            return new RaceSaveLoadResult(RaceSaveLoadStatus.Loaded, saveData, null);
        }

        public static RaceSaveLoadResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Load failure requires an error message.", nameof(errorMessage));
            }

            return new RaceSaveLoadResult(RaceSaveLoadStatus.Failed, null, errorMessage);
        }
    }
}
