namespace ThreadRace.Gameplay.Domain
{
    public sealed class RaceRankingEntry
    {
        public RaceRankingEntry(
            RacerId racerId,
            int currentRank,
            int progress,
            bool isFinished,
            int? finishPlacement)
        {
            RacerId = racerId;
            CurrentRank = currentRank;
            Progress = progress;
            IsFinished = isFinished;
            FinishPlacement = finishPlacement;
        }

        public RacerId RacerId { get; }

        public int CurrentRank { get; }

        public int Progress { get; }

        public bool IsFinished { get; }

        public int? FinishPlacement { get; }
    }
}
