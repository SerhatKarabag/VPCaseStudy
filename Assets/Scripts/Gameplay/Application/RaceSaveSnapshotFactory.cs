using System;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;

namespace ThreadRace.Gameplay.Application
{
    internal static class RaceSaveSnapshotFactory
    {
        public static RaceSaveData Capture(
            int schemaVersion,
            RacePhase phase,
            RacerRuntimeState[] racers,
            RaceFinishTracker finishTracker,
            PlayerRaceOutcome finalOutcome,
            bool rewardClaimed,
            DeterministicRandomState randomState,
            int revision,
            RaceEventTimingState timingState)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            if (finishTracker == null)
            {
                throw new ArgumentNullException(nameof(finishTracker));
            }

            var racerData = new RaceSaveRacerData[racers.Length];
            for (var i = 0; i < racers.Length; i++)
            {
                var racer = racers[i];
                var aiStepTimeRemaining = racer.Definition.RacerType == RacerType.Ai
                    && phase == RacePhase.Running
                    && !racer.IsFinished
                        ? racer.AiStepTimeRemaining
                        : (float?)null;

                racerData[i] = new RaceSaveRacerData(
                    racer.Definition.Id,
                    racer.Progress,
                    racer.IsFinished,
                    racer.HasFinishPlacement ? racer.FinishPlacement : (int?)null,
                    aiStepTimeRemaining);
            }

            return new RaceSaveData(
                schemaVersion,
                phase,
                racerData,
                finishTracker.CreateFinishOrderIds(racers),
                finalOutcome == null ? null : CreateSaveOutcome(finalOutcome),
                randomState,
                revision,
                CreateSaveTimingData(timingState),
                rewardClaimed);
        }

        private static RaceSavePlayerOutcomeData CreateSaveOutcome(PlayerRaceOutcome outcome)
        {
            return new RaceSavePlayerOutcomeData(
                outcome.PlayerId,
                outcome.DidFinish,
                outcome.IsDnf,
                outcome.FinishPlacement,
                outcome.IsRewardEligible,
                outcome.CompletionReason);
        }

        private static RaceSaveTimingData CreateSaveTimingData(RaceEventTimingState timingState)
        {
            if (timingState == null || !timingState.HasStarted)
            {
                return RaceSaveTimingData.NotStarted();
            }

            return RaceSaveTimingData.Started(
                timingState.StartUtc.Value,
                timingState.EndUtc.Value,
                timingState.LastObservedUtc.Value);
        }
    }
}
