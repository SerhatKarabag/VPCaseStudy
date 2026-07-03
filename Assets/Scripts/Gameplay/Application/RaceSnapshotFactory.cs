using System;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Application
{
    internal static class RaceSnapshotFactory
    {
        public static RaceSnapshot Create(
            RaceConfiguration configuration,
            RacePhase phase,
            RacerRuntimeState[] racers,
            int[] rankingOrder,
            RaceFinishTracker finishTracker,
            PlayerRaceOutcome finalOutcome,
            bool rewardClaimed)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            if (rankingOrder == null)
            {
                throw new ArgumentNullException(nameof(rankingOrder));
            }

            if (finishTracker == null)
            {
                throw new ArgumentNullException(nameof(finishTracker));
            }

            var racerSnapshots = new RacerSnapshot[racers.Length];
            for (var i = 0; i < racers.Length; i++)
            {
                var racer = racers[i];
                racerSnapshots[i] = new RacerSnapshot(
                    racer.Definition.Id,
                    racer.Definition.DisplayName,
                    racer.Definition.RacerType,
                    racer.Progress,
                    racer.CurrentRank,
                    racer.IsFinished,
                    racer.HasFinishPlacement ? racer.FinishPlacement : (int?)null);
            }

            var rankingEntries = new RaceRankingEntry[rankingOrder.Length];
            for (var i = 0; i < rankingOrder.Length; i++)
            {
                var racer = racers[rankingOrder[i]];
                rankingEntries[i] = new RaceRankingEntry(
                    racer.Definition.Id,
                    racer.CurrentRank,
                    racer.Progress,
                    racer.IsFinished,
                    racer.HasFinishPlacement ? racer.FinishPlacement : (int?)null);
            }

            return new RaceSnapshot(
                phase,
                configuration.FinishTarget,
                configuration.RewardedPositionCount,
                configuration.RewardTiers,
                racerSnapshots,
                rankingEntries,
                finishTracker.CreateFinishedResults(racers),
                finalOutcome,
                rewardClaimed);
        }
    }
}
