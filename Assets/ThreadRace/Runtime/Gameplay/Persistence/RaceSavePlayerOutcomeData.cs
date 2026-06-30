using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Persistence
{
    public sealed class RaceSavePlayerOutcomeData
    {
        public RaceSavePlayerOutcomeData(
            RacerId playerId,
            bool didFinish,
            bool isDnf,
            int? finishPlacement,
            bool isRewardEligible)
        {
            if (!playerId.IsValid)
            {
                throw new ArgumentException("Saved player outcome requires a valid player ID.", nameof(playerId));
            }

            PlayerId = playerId;
            DidFinish = didFinish;
            IsDnf = isDnf;
            FinishPlacement = finishPlacement;
            IsRewardEligible = isRewardEligible;
        }

        public RacerId PlayerId { get; }

        public bool DidFinish { get; }

        public bool IsDnf { get; }

        public int? FinishPlacement { get; }

        public bool IsRewardEligible { get; }
    }
}
