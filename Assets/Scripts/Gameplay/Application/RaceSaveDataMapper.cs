using System;
using System.Collections.Generic;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;

namespace ThreadRace.Gameplay.Application
{
    public sealed class RaceSaveDataMapper
    {
        public RaceSaveData Capture(RaceEventSettings settings, RaceSession session, RaceEventTimingState timingState = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return session.CaptureSaveData(settings.SaveSchemaVersion, timingState);
        }

        public RaceSession Restore(
            RaceEventSettings settings,
            RaceSaveData saveData,
            IDeterministicRandomSourceFactory randomSourceFactory)
        {
            if (randomSourceFactory == null)
            {
                throw new ArgumentNullException(nameof(randomSourceFactory));
            }

            Validate(settings, saveData);

            try
            {
                var randomSource = randomSourceFactory.Restore(saveData.RandomState);
                return RaceSession.Restore(settings.RaceConfiguration, randomSource, saveData);
            }
            catch (RaceSaveValidationException)
            {
                throw;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                throw new RaceSaveValidationException($"Saved deterministic random state could not be restored: {exception.Message}");
            }
        }

        public void Validate(RaceEventSettings settings, RaceSaveData saveData)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            var configuration = settings.RaceConfiguration;
            if (saveData.SchemaVersion != settings.SaveSchemaVersion)
            {
                Fail($"Unsupported save schema version {saveData.SchemaVersion}. Expected {settings.SaveSchemaVersion}.");
            }

            ValidatePhase(saveData.Phase);

            if (saveData.Racers == null || saveData.FinishOrder == null)
            {
                Fail("Save collections must not be null.");
            }

            if (saveData.Racers.Count != configuration.Racers.Count)
            {
                Fail($"Save contains {saveData.Racers.Count} racers. Expected {configuration.Racers.Count}.");
            }

            var racersByConfigIndex = ValidateRacers(settings, saveData);
            ValidateFinishOrder(configuration, saveData, racersByConfigIndex);
            ValidatePhaseSpecificState(settings, saveData, racersByConfigIndex);
            ValidateTimingState(settings, saveData);
            ValidateRandomState(saveData);
        }

        public RaceEventTimingState RestoreTimingState(RaceEventSettings settings, RaceSaveData saveData)
        {
            Validate(settings, saveData);

            var timing = saveData.TimingData;
            if (!timing.HasStarted)
            {
                return RaceEventTimingState.NotStarted();
            }

            return RaceEventTimingState.Started(
                DateTimeOffset.FromUnixTimeSeconds(timing.StartUtcUnixSeconds),
                DateTimeOffset.FromUnixTimeSeconds(timing.EndUtcUnixSeconds),
                DateTimeOffset.FromUnixTimeSeconds(timing.LastObservedUtcUnixSeconds));
        }

        private static RaceSaveRacerData[] ValidateRacers(RaceEventSettings settings, RaceSaveData saveData)
        {
            var configuration = settings.RaceConfiguration;
            var racersByConfigIndex = new RaceSaveRacerData[configuration.Racers.Count];
            var seenIds = new HashSet<RacerId>();
            var seenPlacements = new HashSet<int>();

            for (var i = 0; i < saveData.Racers.Count; i++)
            {
                var racerData = saveData.Racers[i];
                if (racerData == null)
                {
                    Fail("Save contains a null racer entry.");
                }

                if (!racerData.RacerId.IsValid)
                {
                    Fail("Save contains an invalid racer ID.");
                }

                if (!seenIds.Add(racerData.RacerId))
                {
                    Fail($"Save contains duplicate racer ID '{racerData.RacerId}'.");
                }

                var configIndex = configuration.GetRacerIndex(racerData.RacerId);
                if (configIndex < 0)
                {
                    Fail($"Save contains unexpected racer ID '{racerData.RacerId}'.");
                }

                var definition = configuration.GetRacer(configIndex);
                ValidateRacerProgress(settings, saveData.Phase, definition, racerData);
                ValidateFinishPlacement(racerData, seenPlacements, configuration.Racers.Count);
                racersByConfigIndex[configIndex] = racerData;
            }

            for (var i = 0; i < racersByConfigIndex.Length; i++)
            {
                if (racersByConfigIndex[i] == null)
                {
                    Fail($"Save is missing configured racer ID '{configuration.GetRacer(i).Id}'.");
                }
            }

            return racersByConfigIndex;
        }

        private static void ValidateRacerProgress(
            RaceEventSettings settings,
            RacePhase phase,
            RacerDefinition definition,
            RaceSaveRacerData racerData)
        {
            var finishTarget = settings.RaceConfiguration.FinishTarget;
            if (racerData.Progress < 0)
            {
                Fail($"Racer '{racerData.RacerId}' has negative progress.");
            }

            if (racerData.Progress > finishTarget)
            {
                Fail($"Racer '{racerData.RacerId}' exceeds the finish target.");
            }

            if (racerData.IsFinished && racerData.Progress != finishTarget)
            {
                Fail($"Finished racer '{racerData.RacerId}' is below the finish target.");
            }

            if (!racerData.IsFinished && racerData.Progress >= finishTarget)
            {
                Fail($"Unfinished racer '{racerData.RacerId}' is at or past the finish target.");
            }

            if (definition.RacerType == RacerType.Player && racerData.AiStepTimeRemaining.HasValue)
            {
                Fail("The player racer must not have an AI timer.");
            }

            if (racerData.AiStepTimeRemaining.HasValue)
            {
                var timer = racerData.AiStepTimeRemaining.Value;
                if (float.IsNaN(timer) || float.IsInfinity(timer) || timer <= 0f)
                {
                    Fail($"AI racer '{racerData.RacerId}' has an invalid timer.");
                }

                if (phase != RacePhase.Running)
                {
                    Fail($"AI racer '{racerData.RacerId}' has a timer outside the running phase.");
                }

                if (racerData.IsFinished)
                {
                    Fail($"Finished AI racer '{racerData.RacerId}' must not have a timer.");
                }
            }

            if (phase == RacePhase.Running
                && definition.RacerType == RacerType.Ai
                && !racerData.IsFinished
                && !racerData.AiStepTimeRemaining.HasValue)
            {
                Fail($"Running AI racer '{racerData.RacerId}' is missing a timer.");
            }
        }

        private static void ValidateFinishPlacement(
            RaceSaveRacerData racerData,
            HashSet<int> seenPlacements,
            int racerCount)
        {
            if (racerData.IsFinished)
            {
                if (!racerData.FinishPlacement.HasValue)
                {
                    Fail($"Finished racer '{racerData.RacerId}' is missing a finish placement.");
                }
            }
            else if (racerData.FinishPlacement.HasValue)
            {
                Fail($"Unfinished racer '{racerData.RacerId}' has a finish placement.");
            }

            if (!racerData.FinishPlacement.HasValue)
            {
                return;
            }

            var placement = racerData.FinishPlacement.Value;
            if (placement <= 0 || placement > racerCount)
            {
                Fail($"Racer '{racerData.RacerId}' has invalid finish placement {placement}.");
            }

            if (!seenPlacements.Add(placement))
            {
                Fail($"Duplicate finish placement {placement} exists in the save.");
            }
        }

        private static void ValidateFinishOrder(
            RaceConfiguration configuration,
            RaceSaveData saveData,
            IReadOnlyList<RaceSaveRacerData> racersByConfigIndex)
        {
            var finishedCount = 0;
            var seenFinishers = new HashSet<RacerId>();

            for (var i = 0; i < racersByConfigIndex.Count; i++)
            {
                if (racersByConfigIndex[i].IsFinished)
                {
                    finishedCount++;
                }
            }

            if (saveData.FinishOrder.Count != finishedCount)
            {
                Fail("Finish order count does not match the number of finished racers.");
            }

            for (var i = 0; i < saveData.FinishOrder.Count; i++)
            {
                var racerId = saveData.FinishOrder[i];
                if (!racerId.IsValid)
                {
                    Fail("Finish order contains an invalid racer ID.");
                }

                if (!seenFinishers.Add(racerId))
                {
                    Fail($"Finish order contains duplicate racer ID '{racerId}'.");
                }

                var configIndex = configuration.GetRacerIndex(racerId);
                if (configIndex < 0)
                {
                    Fail($"Finish order contains unknown racer ID '{racerId}'.");
                }

                var racerData = racersByConfigIndex[configIndex];
                if (!racerData.IsFinished)
                {
                    Fail($"Finish order contains unfinished racer '{racerId}'.");
                }

                var expectedPlacement = i + 1;
                if (racerData.FinishPlacement != expectedPlacement)
                {
                    Fail($"Finish order contradicts placement for racer '{racerId}'.");
                }
            }

            for (var placement = 1; placement <= finishedCount; placement++)
            {
                var found = false;
                for (var i = 0; i < racersByConfigIndex.Count; i++)
                {
                    if (racersByConfigIndex[i].FinishPlacement == placement)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Fail($"Finish placement {placement} is missing.");
                }
            }
        }

        private static void ValidatePhaseSpecificState(
            RaceEventSettings settings,
            RaceSaveData saveData,
            IReadOnlyList<RaceSaveRacerData> racersByConfigIndex)
        {
            var playerData = racersByConfigIndex[settings.RaceConfiguration.PlayerRacerIndex];

            if (saveData.Phase == RacePhase.NotStarted)
            {
                if (saveData.RewardClaimed)
                {
                    Fail("NotStarted saves must not contain claimed rewards.");
                }

                if (saveData.PlayerOutcome != null)
                {
                    Fail("NotStarted saves must not contain a final player outcome.");
                }

                if (saveData.FinishOrder.Count > 0)
                {
                    Fail("NotStarted saves must not contain finishers.");
                }

                for (var i = 0; i < racersByConfigIndex.Count; i++)
                {
                    if (racersByConfigIndex[i].Progress != 0 || racersByConfigIndex[i].IsFinished)
                    {
                        Fail("NotStarted saves must not contain progressed racers.");
                    }
                }
            }
            else if (saveData.Phase == RacePhase.Running)
            {
                if (saveData.RewardClaimed)
                {
                    Fail("Running saves must not contain claimed rewards.");
                }

                if (saveData.PlayerOutcome != null)
                {
                    Fail("Running saves must not contain a final player outcome.");
                }

                if (playerData.IsFinished)
                {
                    Fail("Running saves cannot have the player already finished.");
                }

                if (saveData.FinishOrder.Count >= settings.RaceConfiguration.RewardedPositionCount)
                {
                    Fail("Running saves cannot already have an irreversible player DNF outcome.");
                }
            }
            else if (saveData.Phase == RacePhase.Reward)
            {
                if (saveData.RewardClaimed)
                {
                    Fail("Reward-phase saves must not already contain a claimed reward.");
                }

                if (saveData.PlayerOutcome == null)
                {
                    Fail("Reward-phase saves require a final player outcome.");
                }

                ValidatePlayerOutcome(settings, saveData, playerData);
            }
            else if (saveData.Phase == RacePhase.Completed)
            {
                if (saveData.PlayerOutcome == null)
                {
                    Fail("Completed saves require a final player outcome.");
                }

                ValidatePlayerOutcome(settings, saveData, playerData);

                if (saveData.RewardClaimed && !saveData.PlayerOutcome.IsRewardEligible)
                {
                    Fail("Only an eligible finished player reward can be marked as claimed.");
                }

                if (saveData.PlayerOutcome.IsRewardEligible && !saveData.RewardClaimed)
                {
                    Fail("Completed eligible outcomes must have the reward marked as claimed.");
                }
            }
        }

        private static void ValidatePlayerOutcome(
            RaceEventSettings settings,
            RaceSaveData saveData,
            RaceSaveRacerData playerData)
        {
            var outcome = saveData.PlayerOutcome;
            if (outcome.PlayerId != settings.RaceConfiguration.PlayerDefinition.Id)
            {
                Fail("Player outcome is for a racer that is not the configured player.");
            }

            if (outcome.DidFinish)
            {
                if (outcome.IsDnf)
                {
                    Fail("A finished player outcome cannot also be DNF.");
                }

                if (!outcome.FinishPlacement.HasValue)
                {
                    Fail("A finished player outcome is missing a finish placement.");
                }

                if (!playerData.IsFinished || playerData.FinishPlacement != outcome.FinishPlacement)
                {
                    Fail("Player outcome contradicts the player's saved finish placement.");
                }

                var expectedReward = settings.RaceConfiguration.GetRewardTierForRank(outcome.FinishPlacement.Value) != null;
                if (outcome.IsRewardEligible != expectedReward)
                {
                    Fail("Player reward eligibility contradicts the final placement.");
                }
            }
            else
            {
                if (!outcome.IsDnf)
                {
                    Fail("A non-finished completed player outcome must be DNF.");
                }

                if (outcome.FinishPlacement.HasValue)
                {
                    Fail("A DNF player outcome must not have a finish placement.");
                }

                if (playerData.IsFinished)
                {
                    Fail("A DNF player outcome contradicts a finished player state.");
                }

                if (outcome.IsRewardEligible)
                {
                    Fail("A DNF player outcome must not be reward eligible.");
                }

                if (outcome.CompletionReason == RaceCompletionReason.RewardPositionsFilled
                    && saveData.FinishOrder.Count < settings.RaceConfiguration.RewardedPositionCount)
                {
                    Fail("A DNF outcome requires the reward positions to be filled before the player.");
                }
            }

            if (outcome.CompletionReason == RaceCompletionReason.None)
            {
                Fail("Completed saves require an explicit completion reason.");
            }

            if (outcome.CompletionReason == RaceCompletionReason.PlayerFinished && !outcome.DidFinish)
            {
                Fail("PlayerFinished completion reason requires a finished player.");
            }

            if (outcome.CompletionReason == RaceCompletionReason.EventExpired)
            {
                if (outcome.DidFinish || outcome.IsRewardEligible || outcome.FinishPlacement.HasValue)
                {
                    Fail("EventExpired outcome must be DNF with no reward and no placement.");
                }
            }

            if (outcome.CompletionReason == RaceCompletionReason.RewardPositionsFilled && outcome.DidFinish)
            {
                Fail("RewardPositionsFilled outcome cannot be used for a finished player.");
            }
        }

        private static void ValidateTimingState(RaceEventSettings settings, RaceSaveData saveData)
        {
            var timing = saveData.TimingData;
            if (timing == null)
            {
                Fail("Save timing data is missing.");
            }

            if (saveData.Phase == RacePhase.NotStarted)
            {
                if (!timing.HasStarted)
                {
                    return;
                }
            }
            else if (!timing.HasStarted)
            {
                Fail("Started, reward, or completed saves require event timestamps.");
            }

            DateTimeOffset startUtc;
            DateTimeOffset endUtc;
            DateTimeOffset lastObservedUtc;
            try
            {
                startUtc = DateTimeOffset.FromUnixTimeSeconds(timing.StartUtcUnixSeconds);
                endUtc = DateTimeOffset.FromUnixTimeSeconds(timing.EndUtcUnixSeconds);
                lastObservedUtc = DateTimeOffset.FromUnixTimeSeconds(timing.LastObservedUtcUnixSeconds);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                Fail($"Save contains invalid UTC timestamp: {exception.Message}");
                return;
            }

            if (endUtc <= startUtc)
            {
                Fail("Event end UTC must be after start UTC.");
            }

            if (lastObservedUtc < startUtc)
            {
                Fail("Last observed UTC must not be before start UTC.");
            }

            var configuredDurationSeconds = (endUtc - startUtc).TotalSeconds;
            if (Math.Abs(configuredDurationSeconds - settings.EventDurationSeconds) > 1d)
            {
                Fail("Saved event duration is inconsistent with the configured duration.");
            }

            if (saveData.Phase == RacePhase.Running && saveData.PlayerOutcome != null)
            {
                Fail("Running saves must not contain a completion reason.");
            }

            if ((saveData.Phase == RacePhase.Reward || saveData.Phase == RacePhase.Completed)
                && saveData.PlayerOutcome == null)
            {
                Fail("Resolved saves require a completion reason.");
            }

            if (saveData.PlayerOutcome != null
                && saveData.PlayerOutcome.CompletionReason == RaceCompletionReason.EventExpired
                && lastObservedUtc < endUtc)
            {
                Fail("EventExpired saves must observe time at or beyond the event end.");
            }
        }

        private static void ValidateRandomState(RaceSaveData saveData)
        {
            if (saveData.RandomState.ConsumedCount < 0)
            {
                Fail("Saved deterministic random state has a negative consumed count.");
            }

            if (saveData.Phase == RacePhase.NotStarted && saveData.RandomState.ConsumedCount != 0)
            {
                Fail("NotStarted saves must not contain consumed random values.");
            }

        }

        private static void ValidatePhase(RacePhase phase)
        {
            if (phase != RacePhase.NotStarted && phase != RacePhase.Running && phase != RacePhase.Reward && phase != RacePhase.Completed)
            {
                Fail("Save contains an unsupported race phase.");
            }
        }

        private static void Fail(string message)
        {
            throw new RaceSaveValidationException(message);
        }
    }
}
