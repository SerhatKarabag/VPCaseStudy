using NUnit.Framework;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;
using ThreadRace.Infrastructure.Randomness;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceSaveDataMapperTests
    {
        [Test]
        public void NotStartedState_RoundTrips()
        {
            var settings = RaceTestSupport.CreateSettings();
            var session = RaceTestSupport.CreateSession(settings);

            AssertRoundTrip(settings, new RaceSaveDataMapper().Capture(settings, session));
        }

        [Test]
        public void NotStartedState_WithLiveEventWindowRoundTrips()
        {
            var settings = RaceTestSupport.CreateSettings();
            var session = RaceTestSupport.CreateSession(settings);
            var saveData = new RaceSaveDataMapper().Capture(
                settings,
                session,
                RaceTestSupport.CreateStartedTiming(settings));

            AssertRoundTrip(settings, saveData);
        }

        [Test]
        public void RunningState_RoundTrips()
        {
            var settings = RaceTestSupport.CreateSettings(rangedAiDelays: true);
            var saveData = RaceTestSupport.CaptureStartedSave(settings);

            AssertRoundTrip(settings, saveData);
        }

        [Test]
        public void CompletedRewardedPlayerState_RoundTrips()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedRewardedSave();

            AssertRoundTrip(settings, saveData);
        }

        [Test]
        public void RewardPhaseRewardedPlayerState_RoundTrips()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureRewardPhaseRewardedSave();

            Assert.AreEqual(RacePhase.Reward, saveData.Phase);
            Assert.IsFalse(saveData.RewardClaimed);
            AssertRoundTrip(settings, saveData);
        }

        [Test]
        public void CompletedDnfState_RoundTrips()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();

            AssertRoundTrip(settings, saveData);
        }

        [Test]
        public void RacerProgress_IsPreserved()
        {
            var settings = RaceTestSupport.CreateSettings();
            var session = RaceTestSupport.CreateSession(settings);
            session.Start();
            session.ApplyPlayerResult(LevelResult.Success);

            var restored = Restore(settings, new RaceSaveDataMapper().Capture(
                settings,
                session,
                RaceTestSupport.CreateStartedTiming(settings)));

            Assert.AreEqual(
                RaceTestSupport.SnapshotSignature(session.GetSnapshot()),
                RaceTestSupport.SnapshotSignature(restored.GetSnapshot()));
        }

        [Test]
        public void AiCountdowns_ArePreserved()
        {
            var settings = RaceTestSupport.CreateSettings(rangedAiDelays: true);
            var session = RaceTestSupport.CreateSession(settings);
            session.Start();
            session.AdvanceAi(0.2f);

            var saveData = new RaceSaveDataMapper().Capture(
                settings,
                session,
                RaceTestSupport.CreateStartedTiming(settings));
            var restoredSaveData = new RaceSaveDataMapper().Capture(
                settings,
                Restore(settings, saveData),
                new RaceSaveDataMapper().RestoreTimingState(settings, saveData));

            Assert.AreEqual(RaceTestSupport.SaveSignature(saveData), RaceTestSupport.SaveSignature(restoredSaveData));
        }

        [Test]
        public void FinishOrder_IsPreserved()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();
            var restored = Restore(settings, saveData);

            Assert.AreEqual(3, restored.GetSnapshot().Finishers.Count);
            Assert.AreEqual(RaceTestSupport.Ai1Id, restored.GetSnapshot().Finishers[0].RacerId.Value);
            Assert.AreEqual(RaceTestSupport.Ai2Id, restored.GetSnapshot().Finishers[1].RacerId.Value);
            Assert.AreEqual(RaceTestSupport.Ai3Id, restored.GetSnapshot().Finishers[2].RacerId.Value);
        }

        [Test]
        public void FinalPlayerOutcome_IsPreserved()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedRewardedSave();
            var restored = Restore(settings, saveData);

            Assert.IsTrue(restored.GetSnapshot().PlayerOutcome.DidFinish);
            Assert.IsTrue(restored.GetSnapshot().PlayerOutcome.IsRewardEligible);
            Assert.AreEqual(1, restored.GetSnapshot().PlayerOutcome.FinishPlacement);
        }

        [Test]
        public void UnsupportedSaveVersion_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = WithVersion(RaceTestSupport.CaptureStartedSave(settings), 99);

            AssertInvalid(settings, saveData);
        }

        [Test]
        public void DevelopmentV1Save_IsNotTrustedAsTimedEventSave()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = WithVersion(RaceTestSupport.CaptureStartedSave(settings), 1);

            AssertInvalid(settings, saveData);
        }

        [Test]
        public void TimingFields_RoundTrip()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var timingState = new RaceSaveDataMapper().RestoreTimingState(settings, saveData);

            Assert.AreEqual(saveData.TimingData.StartUtcUnixSeconds, timingState.StartUtc.Value.ToUnixTimeSeconds());
            Assert.AreEqual(saveData.TimingData.EndUtcUnixSeconds, timingState.EndUtc.Value.ToUnixTimeSeconds());
            Assert.AreEqual(saveData.TimingData.LastObservedUtcUnixSeconds, timingState.LastObservedUtc.Value.ToUnixTimeSeconds());
        }

        [Test]
        public void RunningSaveWithoutActiveTimestamps_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = WithTiming(RaceTestSupport.CaptureStartedSave(settings), RaceSaveTimingData.NotStarted());

            AssertInvalid(settings, saveData);
        }

        [Test]
        public void EventExpiredBeforeEventEnd_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 100, eventDurationSeconds: 30L);
            var session = RaceTestSupport.CreateSession(settings);
            session.Start();
            session.ExpireEvent();
            var saveData = new RaceSaveDataMapper().Capture(
                settings,
                session,
                RaceTestSupport.CreateStartedTiming(settings));

            AssertInvalid(settings, saveData);
        }

        [Test]
        public void MissingRacer_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = new RaceSaveRacerData[saveData.Racers.Count - 1];
            for (var i = 0; i < racers.Length; i++)
            {
                racers[i] = saveData.Racers[i];
            }

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void UnexpectedRacer_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = CopyRacers(saveData);
            racers[0] = new RaceSaveRacerData(new RacerId("unexpected"), 0, false, null, null);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void DuplicateRacerId_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = CopyRacers(saveData);
            racers[1] = new RaceSaveRacerData(racers[0].RacerId, 0, false, null, racers[1].AiStepTimeRemaining);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void DuplicateFinishPlacement_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();
            var racers = CopyRacers(saveData);
            racers[2] = new RaceSaveRacerData(racers[2].RacerId, 1, true, 1, null);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void DuplicateFinishOrderEntry_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();
            var finishOrder = CopyFinishOrder(saveData);
            finishOrder[1] = finishOrder[0];

            AssertInvalid(settings, WithFinishOrder(saveData, finishOrder));
        }

        [Test]
        public void UnknownFinishOrderEntry_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();
            var finishOrder = CopyFinishOrder(saveData);
            finishOrder[0] = new RacerId("unknown");

            AssertInvalid(settings, WithFinishOrder(saveData, finishOrder));
        }

        [Test]
        public void ProgressBelowZero_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = CopyRacers(saveData);
            racers[0] = new RaceSaveRacerData(racers[0].RacerId, -1, false, null, null);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void ProgressAboveFinishTarget_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = CopyRacers(saveData);
            racers[0] = new RaceSaveRacerData(racers[0].RacerId, 11, false, null, null);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void ContradictoryFinishedProgressState_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = CopyRacers(saveData);
            racers[1] = new RaceSaveRacerData(racers[1].RacerId, 0, true, 1, null);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void InvalidFinishPlacement_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedRewardedSave();
            var racers = CopyRacers(saveData);
            racers[0] = new RaceSaveRacerData(racers[0].RacerId, 1, true, 6, null);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void FinishPlacementGap_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();
            var racers = CopyRacers(saveData);
            var finishOrder = CopyFinishOrder(saveData);
            racers[1] = new RaceSaveRacerData(racers[1].RacerId, 1, true, 1, null);
            racers[2] = new RaceSaveRacerData(racers[2].RacerId, 1, true, 3, null);
            racers[3] = new RaceSaveRacerData(racers[3].RacerId, 1, true, 4, null);
            finishOrder[0] = racers[1].RacerId;
            finishOrder[1] = racers[2].RacerId;
            finishOrder[2] = racers[3].RacerId;

            AssertInvalid(settings, WithRacers(WithFinishOrder(saveData, finishOrder), racers));
        }

        [Test]
        public void ContradictoryFinalOutcome_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedRewardedSave();
            var invalidOutcome = new RaceSavePlayerOutcomeData(
                new RacerId(RaceTestSupport.PlayerId),
                true,
                false,
                1,
                false,
                RaceCompletionReason.PlayerFinished);

            AssertInvalid(settings, WithOutcome(saveData, invalidOutcome));
        }

        [Test]
        public void CompletedPhaseWithoutOutcome_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedRewardedSave();

            AssertInvalid(settings, WithOutcome(saveData, null));
        }

        [Test]
        public void RewardPhaseWithClaimedReward_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureRewardPhaseRewardedSave();

            AssertInvalid(settings, WithRewardClaimed(saveData, true));
        }

        [Test]
        public void ClaimedRewardWithoutEligibleOutcome_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();

            AssertInvalid(settings, WithRewardClaimed(saveData, true));
        }

        [Test]
        public void CompletedEligibleOutcomeWithoutClaimedReward_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedRewardedSave();

            AssertInvalid(settings, WithRewardClaimed(saveData, false));
        }

        [Test]
        public void RunningPhaseWithFinalOutcome_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var invalidOutcome = new RaceSavePlayerOutcomeData(
                new RacerId(RaceTestSupport.PlayerId),
                false,
                true,
                null,
                false,
                RaceCompletionReason.RewardPositionsFilled);

            AssertInvalid(settings, WithOutcome(saveData, invalidOutcome));
        }

        [Test]
        public void AiTimerAssignedToPlayer_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = CopyRacers(saveData);
            racers[0] = new RaceSaveRacerData(racers[0].RacerId, 0, false, null, 1f);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void NegativeAiTimer_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var racers = CopyRacers(saveData);
            racers[1] = new RaceSaveRacerData(racers[1].RacerId, 0, false, null, -0.1f);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void TimerAssignedToFinishedAi_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var saveData = RaceTestSupport.CaptureCompletedDnfSave();
            var racers = CopyRacers(saveData);
            racers[1] = new RaceSaveRacerData(racers[1].RacerId, 1, true, 1, 0.5f);

            AssertInvalid(settings, WithRacers(saveData, racers));
        }

        [Test]
        public void InvalidRandomState_IsRejected()
        {
            var settings = RaceTestSupport.CreateSettings();
            var session = RaceTestSupport.CreateSession(settings);
            var saveData = new RaceSaveDataMapper().Capture(settings, session);
            var invalidRandomState = new DeterministicRandomState(
                saveData.RandomState.Seed,
                saveData.RandomState.State,
                1);

            AssertInvalid(settings, WithRandomState(saveData, invalidRandomState));
        }

        private static RaceSession Restore(RaceEventSettings settings, RaceSaveData saveData)
        {
            return new RaceSaveDataMapper().Restore(settings, saveData, new SeededRandomSourceFactory());
        }

        private static void AssertRoundTrip(RaceEventSettings settings, RaceSaveData saveData)
        {
            var restored = Restore(settings, saveData);
            var restoredSave = new RaceSaveDataMapper().Capture(
                settings,
                restored,
                new RaceSaveDataMapper().RestoreTimingState(settings, saveData));

            Assert.AreEqual(RaceTestSupport.SaveSignature(saveData), RaceTestSupport.SaveSignature(restoredSave));
        }

        private static void AssertInvalid(RaceEventSettings settings, RaceSaveData saveData)
        {
            Assert.Throws<RaceSaveValidationException>(() =>
                new RaceSaveDataMapper().Validate(settings, saveData));
        }

        private static RaceSaveRacerData[] CopyRacers(RaceSaveData saveData)
        {
            var racers = new RaceSaveRacerData[saveData.Racers.Count];
            for (var i = 0; i < racers.Length; i++)
            {
                racers[i] = saveData.Racers[i];
            }

            return racers;
        }

        private static RacerId[] CopyFinishOrder(RaceSaveData saveData)
        {
            var finishOrder = new RacerId[saveData.FinishOrder.Count];
            for (var i = 0; i < finishOrder.Length; i++)
            {
                finishOrder[i] = saveData.FinishOrder[i];
            }

            return finishOrder;
        }

        private static RaceSaveData WithVersion(RaceSaveData source, int schemaVersion)
        {
            return new RaceSaveData(
                schemaVersion,
                source.Phase,
                source.Racers,
                source.FinishOrder,
                source.PlayerOutcome,
                source.RandomState,
                source.Revision,
                source.TimingData,
                source.RewardClaimed);
        }

        private static RaceSaveData WithPhase(RaceSaveData source, RacePhase phase)
        {
            return new RaceSaveData(
                source.SchemaVersion,
                phase,
                source.Racers,
                source.FinishOrder,
                source.PlayerOutcome,
                source.RandomState,
                source.Revision,
                source.TimingData,
                source.RewardClaimed);
        }

        private static RaceSaveData WithRacers(RaceSaveData source, RaceSaveRacerData[] racers)
        {
            return new RaceSaveData(
                source.SchemaVersion,
                source.Phase,
                racers,
                source.FinishOrder,
                source.PlayerOutcome,
                source.RandomState,
                source.Revision,
                source.TimingData,
                source.RewardClaimed);
        }

        private static RaceSaveData WithFinishOrder(RaceSaveData source, RacerId[] finishOrder)
        {
            return new RaceSaveData(
                source.SchemaVersion,
                source.Phase,
                source.Racers,
                finishOrder,
                source.PlayerOutcome,
                source.RandomState,
                source.Revision,
                source.TimingData,
                source.RewardClaimed);
        }

        private static RaceSaveData WithOutcome(RaceSaveData source, RaceSavePlayerOutcomeData outcome)
        {
            return new RaceSaveData(
                source.SchemaVersion,
                source.Phase,
                source.Racers,
                source.FinishOrder,
                outcome,
                source.RandomState,
                source.Revision,
                source.TimingData,
                source.RewardClaimed);
        }

        private static RaceSaveData WithRandomState(RaceSaveData source, DeterministicRandomState randomState)
        {
            return new RaceSaveData(
                source.SchemaVersion,
                source.Phase,
                source.Racers,
                source.FinishOrder,
                source.PlayerOutcome,
                randomState,
                source.Revision,
                source.TimingData,
                source.RewardClaimed);
        }

        private static RaceSaveData WithTiming(RaceSaveData source, RaceSaveTimingData timingData)
        {
            return new RaceSaveData(
                source.SchemaVersion,
                source.Phase,
                source.Racers,
                source.FinishOrder,
                source.PlayerOutcome,
                source.RandomState,
                source.Revision,
                timingData,
                source.RewardClaimed);
        }

        private static RaceSaveData WithRewardClaimed(RaceSaveData source, bool rewardClaimed)
        {
            return new RaceSaveData(
                source.SchemaVersion,
                source.Phase,
                source.Racers,
                source.FinishOrder,
                source.PlayerOutcome,
                source.RandomState,
                source.Revision,
                source.TimingData,
                rewardClaimed);
        }
    }
}
