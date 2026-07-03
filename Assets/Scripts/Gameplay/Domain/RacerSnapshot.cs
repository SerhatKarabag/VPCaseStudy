namespace ThreadRace.Gameplay.Domain
{
    public sealed class RacerSnapshot
    {
        public RacerSnapshot(
            RacerId id,
            string displayName,
            RacerType racerType,
            int progress,
            int currentRank,
            bool isFinished,
            int? finishPlacement)
        {
            Id = id;
            DisplayName = displayName;
            RacerType = racerType;
            Progress = progress;
            CurrentRank = currentRank;
            IsFinished = isFinished;
            FinishPlacement = finishPlacement;
        }

        public RacerId Id { get; }

        public string DisplayName { get; }

        public RacerType RacerType { get; }

        public int Progress { get; }

        public int CurrentRank { get; }

        public bool IsFinished { get; }

        public int? FinishPlacement { get; }
    }
}
