namespace ThreadRace.Gameplay.Domain
{
    public sealed class FinishedRacerResult
    {
        public FinishedRacerResult(RacerId racerId, int finishPlacement)
        {
            RacerId = racerId;
            FinishPlacement = finishPlacement;
        }

        public RacerId RacerId { get; }

        public int FinishPlacement { get; }
    }
}
