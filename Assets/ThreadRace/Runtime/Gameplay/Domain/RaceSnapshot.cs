using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThreadRace.Gameplay.Domain
{
    public sealed class RaceSnapshot
    {
        private readonly ReadOnlyCollection<RacerSnapshot> _racers;
        private readonly ReadOnlyCollection<RaceRankingEntry> _ranking;
        private readonly ReadOnlyCollection<FinishedRacerResult> _finishers;

        public RaceSnapshot(
            RacePhase phase,
            int finishTarget,
            int rewardedPositionCount,
            IEnumerable<RacerSnapshot> racers,
            IEnumerable<RaceRankingEntry> ranking,
            IEnumerable<FinishedRacerResult> finishers,
            PlayerRaceOutcome playerOutcome)
        {
            Phase = phase;
            FinishTarget = finishTarget;
            RewardedPositionCount = rewardedPositionCount;
            _racers = Array.AsReadOnly(CopyRequired(racers, nameof(racers)));
            _ranking = Array.AsReadOnly(CopyRequired(ranking, nameof(ranking)));
            _finishers = Array.AsReadOnly(CopyRequired(finishers, nameof(finishers)));
            PlayerOutcome = playerOutcome;
        }

        public RacePhase Phase { get; }

        public int FinishTarget { get; }

        public int RewardedPositionCount { get; }

        public IReadOnlyList<RacerSnapshot> Racers => _racers;

        public IReadOnlyList<RaceRankingEntry> Ranking => _ranking;

        public IReadOnlyList<FinishedRacerResult> Finishers => _finishers;

        public PlayerRaceOutcome PlayerOutcome { get; }

        private static T[] CopyRequired<T>(IEnumerable<T> source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copied = new List<T>();
            foreach (var item in source)
            {
                if (item == null)
                {
                    throw new ArgumentException("Snapshot collections must not contain null entries.", parameterName);
                }

                copied.Add(item);
            }

            return copied.ToArray();
        }
    }
}
