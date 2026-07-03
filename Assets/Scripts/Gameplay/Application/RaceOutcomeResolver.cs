using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Application
{
    internal static class RaceOutcomeResolver
    {
        public static PlayerRaceOutcome ResolveIfNeeded(
            RacePhase phase,
            RacerRuntimeState[] racers,
            int playerIndex,
            int rewardedPositionCount,
            RaceFinishTracker finishTracker)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            if (finishTracker == null)
            {
                throw new ArgumentNullException(nameof(finishTracker));
            }

            if (phase != RacePhase.Running)
            {
                return null;
            }

            if (playerIndex < 0 || playerIndex >= racers.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            var player = racers[playerIndex];
            if (player.IsFinished)
            {
                return new PlayerRaceOutcome(
                    player.Definition.Id,
                    true,
                    false,
                    player.FinishPlacement,
                    player.FinishPlacement <= rewardedPositionCount,
                    RaceCompletionReason.PlayerFinished,
                    finishTracker.CreateFinishedResults(racers));
            }

            if (finishTracker.FinishCount >= rewardedPositionCount)
            {
                return new PlayerRaceOutcome(
                    player.Definition.Id,
                    false,
                    true,
                    null,
                    false,
                    RaceCompletionReason.RewardPositionsFilled,
                    finishTracker.CreateFinishedResults(racers));
            }

            return null;
        }

        public static PlayerRaceOutcome CreateExpiredOutcome(
            RacerRuntimeState[] racers,
            int playerIndex,
            RaceFinishTracker finishTracker)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            if (finishTracker == null)
            {
                throw new ArgumentNullException(nameof(finishTracker));
            }

            if (playerIndex < 0 || playerIndex >= racers.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            var player = racers[playerIndex];
            return new PlayerRaceOutcome(
                player.Definition.Id,
                false,
                true,
                null,
                false,
                RaceCompletionReason.EventExpired,
                finishTracker.CreateFinishedResults(racers));
        }
    }
}
