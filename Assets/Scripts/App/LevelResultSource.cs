using System;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.App
{
    public sealed class LevelResultSource : ILevelResultSource, ILevelResultReporter
    {
        public event Action<LevelResult> ResultReported;

        public void Report(LevelResult result)
        {
            ValidateLevelResult(result);
            ResultReported?.Invoke(result);
        }

        private static void ValidateLevelResult(LevelResult result)
        {
            if (result != LevelResult.Success && result != LevelResult.Fail)
            {
                throw new ArgumentOutOfRangeException(nameof(result), "Unsupported level result.");
            }
        }
    }
}
