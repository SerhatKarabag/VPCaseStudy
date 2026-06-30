using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Persistence
{
    public sealed class RaceSaveRacerData
    {
        public RaceSaveRacerData(
            RacerId racerId,
            int progress,
            bool isFinished,
            int? finishPlacement,
            float? aiStepTimeRemaining)
        {
            if (!racerId.IsValid)
            {
                throw new ArgumentException("Saved racer data requires a valid racer ID.", nameof(racerId));
            }

            RacerId = racerId;
            Progress = progress;
            IsFinished = isFinished;
            FinishPlacement = finishPlacement;
            AiStepTimeRemaining = aiStepTimeRemaining;
        }

        public RacerId RacerId { get; }

        public int Progress { get; }

        public bool IsFinished { get; }

        public int? FinishPlacement { get; }

        public float? AiStepTimeRemaining { get; }
    }
}
