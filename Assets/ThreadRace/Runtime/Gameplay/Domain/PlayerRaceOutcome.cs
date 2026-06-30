using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThreadRace.Gameplay.Domain
{
    public sealed class PlayerRaceOutcome
    {
        private readonly ReadOnlyCollection<FinishedRacerResult> _finishers;

        public PlayerRaceOutcome(
            RacerId playerId,
            bool didFinish,
            bool isDnf,
            int? finishPlacement,
            bool isRewardEligible,
            IEnumerable<FinishedRacerResult> finishers)
        {
            if (!playerId.IsValid)
            {
                throw new ArgumentException("Player outcome requires a valid player racer ID.", nameof(playerId));
            }

            if (didFinish && !finishPlacement.HasValue)
            {
                throw new ArgumentException("A finished player outcome requires a finish placement.", nameof(finishPlacement));
            }

            if (!didFinish && finishPlacement.HasValue)
            {
                throw new ArgumentException("A non-finished player outcome must not have a finish placement.", nameof(finishPlacement));
            }

            if (didFinish && isDnf)
            {
                throw new ArgumentException("A player cannot both finish and be marked DNF.", nameof(isDnf));
            }

            if (isRewardEligible && !didFinish)
            {
                throw new ArgumentException("A player cannot be reward eligible without finishing.", nameof(isRewardEligible));
            }

            PlayerId = playerId;
            DidFinish = didFinish;
            IsDnf = isDnf;
            FinishPlacement = finishPlacement;
            IsRewardEligible = isRewardEligible;
            _finishers = Array.AsReadOnly(CopyFinishers(finishers));
        }

        public RacerId PlayerId { get; }

        public bool DidFinish { get; }

        public bool IsDnf { get; }

        public int? FinishPlacement { get; }

        public bool IsRewardEligible { get; }

        public IReadOnlyList<FinishedRacerResult> Finishers => _finishers;

        private static FinishedRacerResult[] CopyFinishers(IEnumerable<FinishedRacerResult> finishers)
        {
            if (finishers == null)
            {
                return new FinishedRacerResult[0];
            }

            var copied = new List<FinishedRacerResult>();
            foreach (var finisher in finishers)
            {
                if (finisher == null)
                {
                    throw new ArgumentException("Finished racer results must not contain null entries.", nameof(finishers));
                }

                copied.Add(finisher);
            }

            return copied.ToArray();
        }
    }
}
