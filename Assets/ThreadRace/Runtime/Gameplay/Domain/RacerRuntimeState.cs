using System;
using ThreadRace.Gameplay.Config;

namespace ThreadRace.Gameplay.Domain
{
    internal sealed class RacerRuntimeState
    {
        public RacerRuntimeState(RacerDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public RacerDefinition Definition { get; }

        public int Progress { get; private set; }

        public int CurrentRank { get; set; }

        public bool IsFinished { get; private set; }

        public int FinishPlacement { get; private set; }

        public float AiStepTimeRemaining { get; set; }

        public bool HasFinishPlacement => FinishPlacement > 0;

        public bool AdvanceOneStep(int finishTarget)
        {
            if (finishTarget <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finishTarget));
            }

            if (IsFinished || Progress >= finishTarget)
            {
                return false;
            }

            Progress++;
            if (Progress > finishTarget)
            {
                Progress = finishTarget;
            }

            return true;
        }

        public void RecordFinished(int placement)
        {
            if (placement <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(placement), "Finish placement must be greater than zero.");
            }

            if (IsFinished)
            {
                throw new InvalidOperationException($"Racer '{Definition.Id}' already has a finish placement.");
            }

            IsFinished = true;
            FinishPlacement = placement;
        }
    }
}
