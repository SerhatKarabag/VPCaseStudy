using System;
using NUnit.Framework;
using ThreadRace.App;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;
using ThreadRace.Infrastructure.Randomness;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceTimedEventTests
    {
        [Test]
        public void RuntimeSettings_UseConfiguredDurationAndOneSecondCountdown()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 1800L);

            Assert.AreEqual(1800L, settings.EventDurationSeconds);
            Assert.AreEqual(1, settings.CountdownUpdateIntervalSeconds);
            Assert.AreEqual(3, settings.SaveSchemaVersion);
            Assert.AreEqual("ThreadRace.Tests.Save.V3", settings.SaveKey);
        }

        [Test]
        public void RuntimeSettings_InvalidDurationOrCountdownIntervalAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RaceTestSupport.CreateSettings(eventDurationSeconds: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RaceTestSupport.CreateSettings(eventDurationSeconds: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RaceTestSupport.CreateSettings(countdownUpdateIntervalSeconds: 0));
        }

        [Test]
        public void NotStartedCountdownUsesLiveEventWindow()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 42L);
            var clock = new RaceTestSupport.FakeUtcClock();
            var controller = CreateController(settings, new RaceTestSupport.InMemoryRaceSaveRepository(), clock);

            Assert.IsTrue(controller.CurrentCountdown.IsActive);
            Assert.AreEqual(42L, controller.CurrentCountdown.RemainingSeconds);
            Assert.IsTrue(controller.TimingState.HasStarted);
            Assert.AreEqual(clock.UtcNow, controller.TimingState.StartUtc);
            Assert.AreEqual(clock.UtcNow.AddSeconds(42L), controller.TimingState.EndUtc);
            Assert.AreEqual(RacePhase.NotStarted, controller.Phase);
        }

        [Test]
        public void CurrentCountdownReusesSnapshotWhileDisplayedValuesAreUnchanged()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 42L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var controller = CreateController(settings, new RaceTestSupport.InMemoryRaceSaveRepository(), clock);

            var first = controller.CurrentCountdown;
            var second = controller.CurrentCountdown;
            clock.AdvanceSeconds(1L);
            var third = controller.CurrentCountdown;
            var fourth = controller.CurrentCountdown;

            Assert.AreSame(first, second);
            Assert.AreNotSame(first, third);
            Assert.AreSame(third, fourth);
            Assert.AreEqual(41L, third.RemainingSeconds);
        }

        [Test]
        public void StartCreatesUtcStartEndAndLastObservedTimestamps()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 42L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);

            Assert.IsTrue(controller.StartNewRace());

            Assert.IsTrue(controller.TimingState.HasStarted);
            Assert.AreEqual(clock.UtcNow, controller.TimingState.StartUtc);
            Assert.AreEqual(clock.UtcNow.AddSeconds(42L), controller.TimingState.EndUtc);
            Assert.AreEqual(clock.UtcNow, controller.TimingState.LastObservedUtc);
            Assert.AreEqual(42L, controller.CurrentCountdown.RemainingSeconds);
            Assert.AreEqual(3, repository.LastSavedData.SchemaVersion);
            Assert.IsTrue(repository.LastSavedData.TimingData.HasStarted);
        }

        [Test]
        public void StartDoesNotExtendExistingLiveEventWindow()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 42L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var controller = CreateController(settings, new RaceTestSupport.InMemoryRaceSaveRepository(), clock);
            var originalStart = controller.TimingState.StartUtc;
            var originalEnd = controller.TimingState.EndUtc;

            clock.AdvanceSeconds(12L);

            Assert.IsTrue(controller.StartNewRace());
            Assert.AreEqual(originalStart, controller.TimingState.StartUtc);
            Assert.AreEqual(originalEnd, controller.TimingState.EndUtc);
            Assert.AreEqual(30L, controller.CurrentCountdown.RemainingSeconds);
            Assert.AreEqual(clock.UtcNow, controller.TimingState.LastObservedUtc);
        }

        [Test]
        public void StartFailsAfterLiveEventWindowExpires()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 5L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);

            clock.AdvanceSeconds(5L);

            Assert.IsFalse(controller.StartNewRace());
            Assert.AreEqual(RacePhase.NotStarted, controller.Phase);
            Assert.AreEqual(0L, controller.CurrentCountdown.RemainingSeconds);
            Assert.AreEqual(0, repository.SaveCount);
        }

        [Test]
        public void ExpiredNotStartedEventCanResolveToDnfResult()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 5L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);

            clock.AdvanceSeconds(5L);

            Assert.IsTrue(controller.ResolveExpiredEventIfNeeded());
            Assert.AreEqual(RacePhase.Reward, controller.Phase);
            Assert.AreEqual(RaceCompletionReason.EventExpired, controller.CurrentSnapshot.PlayerOutcome.CompletionReason);
            Assert.IsTrue(controller.CurrentSnapshot.PlayerOutcome.IsDnf);
            Assert.IsFalse(controller.CurrentSnapshot.PlayerOutcome.IsRewardEligible);
            Assert.AreEqual(1, repository.SaveCount);
            Assert.AreEqual(
                controller.TimingState.EndUtc.Value.ToUnixTimeSeconds(),
                repository.LastSavedData.TimingData.LastObservedUtcUnixSeconds);
        }

        [Test]
        public void RepeatedStartDoesNotExtendRunningEvent()
        {
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var controller = CreateController(RaceTestSupport.CreateSettings(), new RaceTestSupport.InMemoryRaceSaveRepository(), clock);
            controller.StartNewRace();
            var originalEnd = controller.TimingState.EndUtc;

            clock.AdvanceSeconds(60L);

            Assert.IsFalse(controller.StartNewRace());
            Assert.AreEqual(originalEnd, controller.TimingState.EndUtc);
        }

        [Test]
        public void ClockRollbackDoesNotMoveObservedTimeBackwardsOrAdvanceOffline()
        {
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var controller = CreateController(RaceTestSupport.CreateSettings(), new RaceTestSupport.InMemoryRaceSaveRepository(), clock);
            controller.StartNewRace();
            var signatureBefore = RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot);

            clock.UtcNow = RaceTestSupport.DefaultStartUtc.AddSeconds(-120L);

            Assert.IsFalse(controller.ApplyOfflineProgression());
            Assert.AreEqual(RaceTestSupport.DefaultStartUtc, controller.TimingState.LastObservedUtc);
            Assert.AreEqual(signatureBefore, RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot));
        }

        [Test]
        public void OfflineCatchUpMatchesUninterruptedAiSimulation()
        {
            var settings = RaceTestSupport.CreateSettings(rangedAiDelays: true);
            var offlineClock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var offlineRepository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var offlineController = CreateController(settings, offlineRepository, offlineClock);
            offlineController.StartNewRace();

            offlineClock.AdvanceSeconds(3L);
            var restoredController = CreateController(settings, offlineRepository, offlineClock);

            var uninterruptedRepository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var uninterruptedClock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var uninterruptedController = CreateController(settings, uninterruptedRepository, uninterruptedClock);
            uninterruptedController.StartNewRace();
            uninterruptedController.AdvanceAi(3f);

            Assert.AreEqual(
                RaceTestSupport.SnapshotSignature(uninterruptedController.CurrentSnapshot),
                RaceTestSupport.SnapshotSignature(restoredController.CurrentSnapshot));
            Assert.AreEqual(uninterruptedRepository.LastSavedData.RandomState, offlineRepository.LastSavedData.RandomState);
        }

        [Test]
        public void OfflineCatchUpNeverAdvancesPlayer()
        {
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(RaceTestSupport.CreateSettings(), repository, clock);
            controller.StartNewRace();

            clock.AdvanceSeconds(5L);
            controller = CreateController(RaceTestSupport.CreateSettings(), repository, clock);

            Assert.AreEqual(0, controller.CurrentSnapshot.Racers[0].Progress);
        }

        [Test]
        public void OfflineCatchUpDoesNotRunBeforeStart()
        {
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc.AddDays(1));
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(RaceTestSupport.CreateSettings(), repository, clock);

            Assert.IsFalse(controller.ApplyOfflineProgression());
            Assert.AreEqual(RacePhase.NotStarted, controller.Phase);
            Assert.AreEqual(0, repository.SaveCount);
        }

        [Test]
        public void OfflineCatchUpIsNotAppliedTwiceForSameObservation()
        {
            var settings = RaceTestSupport.CreateSettings();
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);
            controller.StartNewRace();
            clock.AdvanceSeconds(3L);
            controller = CreateController(settings, repository, clock);
            var signatureAfterFirstCatchUp = RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot);
            var saveCountAfterFirstCatchUp = repository.SaveCount;

            controller = CreateController(settings, repository, clock);

            Assert.AreEqual(signatureAfterFirstCatchUp, RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot));
            Assert.AreEqual(saveCountAfterFirstCatchUp, repository.SaveCount);
        }

        [Test]
        public void OfflineCatchUpCanResolveDnfWhenRewardSlotsFill()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1, eventDurationSeconds: 1000L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);
            controller.StartNewRace();

            clock.AdvanceSeconds(1L);
            controller = CreateController(settings, repository, clock);

            Assert.AreEqual(RacePhase.Reward, controller.Phase);
            Assert.AreEqual(RaceCompletionReason.RewardPositionsFilled, controller.CurrentSnapshot.PlayerOutcome.CompletionReason);
            Assert.IsTrue(controller.CurrentSnapshot.PlayerOutcome.IsDnf);
            Assert.IsFalse(controller.CurrentSnapshot.PlayerOutcome.IsRewardEligible);
            Assert.AreEqual(0, controller.CurrentSnapshot.Racers[0].Progress);
        }

        [Test]
        public void OfflineCatchUpStopsAtEventEndAndExpiresUnfinishedPlayer()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 100, eventDurationSeconds: 5L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);
            controller.StartNewRace();

            clock.AdvanceSeconds(1000L);
            controller = CreateController(settings, repository, clock);

            Assert.AreEqual(RacePhase.Reward, controller.Phase);
            Assert.AreEqual(RaceCompletionReason.EventExpired, controller.CurrentSnapshot.PlayerOutcome.CompletionReason);
            Assert.AreEqual(5, controller.CurrentSnapshot.Racers[1].Progress);
            Assert.AreEqual(0, controller.CurrentCountdown.RemainingSeconds);
            Assert.IsFalse(controller.CurrentSnapshot.PlayerOutcome.IsRewardEligible);
            Assert.IsFalse(controller.CurrentSnapshot.PlayerOutcome.FinishPlacement.HasValue);
        }

        [Test]
        public void CompletedPlayerResultIsPreservedAfterEventEnd()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1, eventDurationSeconds: 5L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var controller = CreateController(settings, new RaceTestSupport.InMemoryRaceSaveRepository(), clock);
            controller.StartNewRace();
            controller.ReportLevelResult(LevelResult.Success);

            clock.AdvanceSeconds(100L);

            Assert.IsFalse(controller.ApplyOfflineProgression());
            Assert.AreEqual(RaceCompletionReason.PlayerFinished, controller.CurrentSnapshot.PlayerOutcome.CompletionReason);
            Assert.IsTrue(controller.CurrentSnapshot.PlayerOutcome.IsRewardEligible);
        }

        [Test]
        public void EventExpiredResultRoundTripsThroughSaveData()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 100, eventDurationSeconds: 5L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);
            controller.StartNewRace();
            clock.AdvanceSeconds(5L);
            controller.ResolveExpirationIfNeeded();

            var mapper = new RaceSaveDataMapper();
            mapper.Validate(settings, repository.LastSavedData);
            var restored = mapper.Restore(settings, repository.LastSavedData, new SeededRandomSourceFactory());
            var restoredTiming = mapper.RestoreTimingState(settings, repository.LastSavedData);

            Assert.AreEqual(RacePhase.Reward, restored.Phase);
            Assert.AreEqual(RaceCompletionReason.EventExpired, restored.GetSnapshot().PlayerOutcome.CompletionReason);
            Assert.AreEqual(repository.LastSavedData.TimingData.EndUtcUnixSeconds, restoredTiming.LastObservedUtc.Value.ToUnixTimeSeconds());
        }

        [Test]
        public void TimeDriverPublishesCountdownAtConfiguredInterval()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 10L, countdownUpdateIntervalSeconds: 2);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);
            var countdownPublisher = new CountingCountdownPublisher();
            var driver = new RaceEventTimeDriver(controller, settings, new CountingSnapshotPublisher(), countdownPublisher);
            controller.StartNewRace();

            driver.Tick();
            clock.AdvanceSeconds(1L);
            driver.Tick();
            clock.AdvanceSeconds(1L);
            driver.Tick();

            Assert.AreEqual(2, countdownPublisher.PublishCount);
            Assert.AreEqual(1, repository.SaveCount);
        }

        [Test]
        public void TimeDriverPublishesCountdownBeforeStartAndAfterStart()
        {
            var settings = RaceTestSupport.CreateSettings(eventDurationSeconds: 10L, countdownUpdateIntervalSeconds: 1);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);
            var countdownPublisher = new CountingCountdownPublisher();
            var driver = new RaceEventTimeDriver(controller, settings, new CountingSnapshotPublisher(), countdownPublisher);

            driver.Tick();
            Assert.AreEqual(1, countdownPublisher.PublishCount);
            Assert.AreEqual(10L, countdownPublisher.LastSnapshot.RemainingSeconds);

            controller.StartNewRace();
            driver.Tick();
            clock.AdvanceSeconds(1L);
            driver.Tick();

            Assert.AreEqual(2, countdownPublisher.PublishCount);
            Assert.AreEqual(9L, countdownPublisher.LastSnapshot.RemainingSeconds);
        }

        [Test]
        public void TimeDriverResolvesExpirationOnceAndStopsAfterCompletion()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 100, eventDurationSeconds: 1L);
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository, clock);
            var snapshotPublisher = new CountingSnapshotPublisher();
            var countdownPublisher = new CountingCountdownPublisher();
            var driver = new RaceEventTimeDriver(controller, settings, snapshotPublisher, countdownPublisher);
            controller.StartNewRace();
            var saveCountAfterStart = repository.SaveCount;

            clock.AdvanceSeconds(1L);
            driver.Tick();
            driver.Tick();

            Assert.AreEqual(RacePhase.Reward, controller.Phase);
            Assert.AreEqual(RaceCompletionReason.EventExpired, controller.CurrentSnapshot.PlayerOutcome.CompletionReason);
            Assert.AreEqual(saveCountAfterStart + 1, repository.SaveCount);
            Assert.AreEqual(1, snapshotPublisher.PublishCount);
        }

        [Test]
        public void CheckpointPersistsLastObservedUtcWithoutGameplayProgression()
        {
            var clock = new RaceTestSupport.FakeUtcClock(RaceTestSupport.DefaultStartUtc);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(RaceTestSupport.CreateSettings(), repository, clock);
            controller.StartNewRace();
            var saveCountAfterStart = repository.SaveCount;

            clock.AdvanceSeconds(30L);

            Assert.IsTrue(controller.CheckpointObservedTime());
            Assert.AreEqual(saveCountAfterStart + 1, repository.SaveCount);
            Assert.AreEqual(clock.UtcNow.ToUnixTimeSeconds(), repository.LastSavedData.TimingData.LastObservedUtcUnixSeconds);
            Assert.AreEqual(0, controller.CurrentSnapshot.Racers[0].Progress);
        }

        private static RaceEventController CreateController(
            RaceEventSettings settings,
            RaceTestSupport.InMemoryRaceSaveRepository repository,
            RaceTestSupport.FakeUtcClock clock)
        {
            return new RaceEventController(
                settings,
                repository,
                new RaceSaveDataMapper(),
                new SeededRandomSourceFactory(),
                clock);
        }

        private sealed class CountingSnapshotPublisher : IRaceSnapshotPublisher
        {
            public int PublishCount { get; private set; }

            public void Publish(RaceSnapshot snapshot)
            {
                PublishCount++;
            }
        }

        private sealed class CountingCountdownPublisher : IRaceCountdownPublisher
        {
            public int PublishCount { get; private set; }

            public RaceCountdownSnapshot LastSnapshot { get; private set; }

            public void Publish(RaceCountdownSnapshot snapshot)
            {
                LastSnapshot = snapshot;
                PublishCount++;
            }
        }
    }
}
