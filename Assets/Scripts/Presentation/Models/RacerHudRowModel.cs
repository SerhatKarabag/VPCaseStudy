namespace ThreadRace.Presentation.Models
{
    public sealed class RacerHudRowModel
    {
        public RacerHudRowModel(
            string racerId,
            string displayName,
            bool isPlayer,
            int currentRank,
            int progress,
            int finishTarget,
            string progressText,
            float normalizedProgress,
            bool isFinished,
            int? finishPlacement,
            int targetSlotIndex)
        {
            RacerId = racerId;
            DisplayName = displayName;
            IsPlayer = isPlayer;
            CurrentRank = currentRank;
            Progress = progress;
            FinishTarget = finishTarget;
            ProgressText = progressText;
            NormalizedProgress = normalizedProgress;
            IsFinished = isFinished;
            FinishPlacement = finishPlacement;
            TargetSlotIndex = targetSlotIndex;
        }

        public string RacerId { get; }

        public string DisplayName { get; }

        public bool IsPlayer { get; }

        public int CurrentRank { get; }

        public int Progress { get; }

        public int FinishTarget { get; }

        public string ProgressText { get; }

        public float NormalizedProgress { get; }

        public bool IsFinished { get; }

        public int? FinishPlacement { get; }

        public int TargetSlotIndex { get; }
    }
}
