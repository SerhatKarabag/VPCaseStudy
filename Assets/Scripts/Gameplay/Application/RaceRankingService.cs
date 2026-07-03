using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Application
{
    internal static class RaceRankingService
    {
        public static void Recalculate(RacerRuntimeState[] racers, int[] rankingOrder)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            if (rankingOrder == null)
            {
                throw new ArgumentNullException(nameof(rankingOrder));
            }

            if (rankingOrder.Length != racers.Length)
            {
                throw new ArgumentException("Ranking order length must match racer count.", nameof(rankingOrder));
            }

            for (var i = 0; i < rankingOrder.Length; i++)
            {
                rankingOrder[i] = i;
            }

            for (var i = 1; i < rankingOrder.Length; i++)
            {
                var candidate = rankingOrder[i];
                var previousIndex = i - 1;

                while (previousIndex >= 0 && CompareRacers(racers, candidate, rankingOrder[previousIndex]) < 0)
                {
                    rankingOrder[previousIndex + 1] = rankingOrder[previousIndex];
                    previousIndex--;
                }

                rankingOrder[previousIndex + 1] = candidate;
            }

            for (var rankIndex = 0; rankIndex < rankingOrder.Length; rankIndex++)
            {
                racers[rankingOrder[rankIndex]].CurrentRank = rankIndex + 1;
            }
        }

        private static int CompareRacers(RacerRuntimeState[] racers, int leftIndex, int rightIndex)
        {
            var left = racers[leftIndex];
            var right = racers[rightIndex];

            if (left.IsFinished && right.IsFinished)
            {
                return left.FinishPlacement.CompareTo(right.FinishPlacement);
            }

            if (left.IsFinished)
            {
                return -1;
            }

            if (right.IsFinished)
            {
                return 1;
            }

            var progressComparison = right.Progress.CompareTo(left.Progress);
            if (progressComparison != 0)
            {
                return progressComparison;
            }

            return left.Definition.InitialOrder.CompareTo(right.Definition.InitialOrder);
        }
    }
}
