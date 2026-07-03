using NUnit.Framework;
using ThreadRace.App;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Infrastructure.Randomness;
using ThreadRace.Presentation.Signals;
using Zenject;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RacePresentationCommandTests
    {
        [Test]
        public void StartCommandPublishesResultingSnapshot()
        {
            var context = CreateContext();

            Assert.IsTrue(context.Router.StartRace());

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.IsTrue(context.LastCountdownSignal.Snapshot.IsActive);
            Assert.AreEqual(RacePhase.Running, context.LastSignal.Snapshot.Phase);
        }

        [Test]
        public void PlayerSuccessPublishesResultingSnapshot()
        {
            var context = CreateContext();
            context.Router.StartRace();
            context.ResetSignalCount();

            Assert.IsTrue(context.Router.ReportLevelResult(LevelResult.Success));

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.AreEqual(1, context.LastSignal.Snapshot.Racers[0].Progress);
        }

        [Test]
        public void PlayerFailPublishesUiRefreshSnapshotWithoutMutation()
        {
            var context = CreateContext();
            context.Router.StartRace();
            context.ResetSignalCount();

            Assert.IsFalse(context.Router.ReportLevelResult(LevelResult.Fail));

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.AreEqual(0, context.LastSignal.Snapshot.Racers[0].Progress);
        }

        [Test]
        public void LevelResultListener_ForwardsSourceEventsThroughRaceRouter()
        {
            var context = CreateContext();
            var source = new LevelResultSource();
            var listener = new RaceLevelResultListener(source, context.Router);
            listener.Initialize();
            context.Router.StartRace();
            context.ResetSignalCount();

            source.Report(LevelResult.Success);

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.AreEqual(1, context.LastSignal.Snapshot.Racers[0].Progress);

            listener.Dispose();
            context.ResetSignalCount();
            source.Report(LevelResult.Success);

            Assert.AreEqual(0, context.SignalCount);
        }

        [Test]
        public void ResetPublishesNotStartedSnapshot()
        {
            var context = CreateContext();
            context.Router.StartRace();
            context.ResetSignalCount();

            context.Router.ResetRace();

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.AreEqual(RacePhase.NotStarted, context.LastSignal.Snapshot.Phase);
        }

        [Test]
        public void ResolveExpiredEventPublishesRewardSnapshot()
        {
            var context = CreateContext(RaceTestSupport.CreateSettings(eventDurationSeconds: 5L));
            context.Clock.AdvanceSeconds(5L);

            Assert.IsTrue(context.Router.ResolveExpiredEvent());

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.AreEqual(RacePhase.Reward, context.LastSignal.Snapshot.Phase);
            Assert.AreEqual(RaceCompletionReason.EventExpired, context.LastSignal.Snapshot.PlayerOutcome.CompletionReason);
            Assert.IsTrue(context.LastSignal.Snapshot.PlayerOutcome.IsDnf);
            Assert.IsFalse(context.LastSignal.Snapshot.PlayerOutcome.IsRewardEligible);
            Assert.IsTrue(context.LastCountdownSignal.Snapshot.IsExpired);
        }

        [Test]
        public void ClaimRewardPublishesCompletedSnapshot()
        {
            var context = CreateContext(RaceTestSupport.CreateSettings(finishTarget: 1));
            context.Router.StartRace();
            context.Router.ReportLevelResult(LevelResult.Success);
            context.ResetSignalCount();

            Assert.IsTrue(context.Router.ClaimReward());

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.AreEqual(RacePhase.Completed, context.LastSignal.Snapshot.Phase);
            Assert.IsTrue(context.LastSignal.Snapshot.RewardClaimed);
        }

        [Test]
        public void AiMutationPublishesSnapshot()
        {
            var context = CreateContext();
            var driver = new RaceSimulationDriver(
                context.Controller,
                new RaceTestSupport.FakeRaceTimeProvider { UnscaledDeltaTime = 1f },
                context.Publisher,
                context.CountdownPublisher);
            context.Router.StartRace();
            context.ResetSignalCount();

            driver.Tick();

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.Greater(context.LastSignal.Snapshot.Ranking[0].Progress, 0);
        }

        [Test]
        public void AiTickWithoutMutationDoesNotPublishEveryFrame()
        {
            var context = CreateContext();
            var driver = new RaceSimulationDriver(
                context.Controller,
                new RaceTestSupport.FakeRaceTimeProvider { UnscaledDeltaTime = 0.5f },
                context.Publisher,
                context.CountdownPublisher);
            context.Router.StartRace();
            context.ResetSignalCount();

            driver.Tick();

            Assert.AreEqual(0, context.SignalCount);
            Assert.AreEqual(0, context.CountdownSignalCount);
        }

        [Test]
        public void BootstrapPublishesCurrentSnapshot()
        {
            var context = CreateContext();
            var bootstrap = new RacePresentationBootstrap(
                context.Router,
                context.Router,
                context.Publisher,
                context.CountdownPublisher);

            bootstrap.Initialize();

            Assert.AreEqual(1, context.SignalCount);
            Assert.AreEqual(1, context.CountdownSignalCount);
            Assert.AreEqual(RacePhase.NotStarted, context.LastSignal.Snapshot.Phase);
        }

        private static CommandTestContext CreateContext(RaceEventSettings settings = null)
        {
            var bus = CreateSignalBus();
            settings = settings ?? RaceTestSupport.CreateSettings();
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var clock = new RaceTestSupport.FakeUtcClock();
            var controller = new RaceEventController(
                settings,
                repository,
                new RaceSaveDataMapper(),
                new SeededRandomSourceFactory(),
                clock);
            var publisher = new RaceSnapshotPublisher(bus);
            var countdownPublisher = new RaceCountdownPublisher(bus);
            var router = new RaceUiCommandRouter(controller, publisher, countdownPublisher);
            return new CommandTestContext(bus, controller, publisher, countdownPublisher, router, clock);
        }

        private static SignalBus CreateSignalBus()
        {
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<RaceSnapshotChangedSignal>();
            container.DeclareSignal<RaceCountdownChangedSignal>();
            return container.Resolve<SignalBus>();
        }

        private sealed class CommandTestContext
        {
            public readonly RaceEventController Controller;
            public readonly RaceSnapshotPublisher Publisher;
            public readonly RaceCountdownPublisher CountdownPublisher;
            public readonly RaceUiCommandRouter Router;
            public readonly RaceTestSupport.FakeUtcClock Clock;
            public RaceSnapshotChangedSignal LastSignal;
            public RaceCountdownChangedSignal LastCountdownSignal;
            public int SignalCount;
            public int CountdownSignalCount;

            public CommandTestContext(
                SignalBus signalBus,
                RaceEventController controller,
                RaceSnapshotPublisher publisher,
                RaceCountdownPublisher countdownPublisher,
                RaceUiCommandRouter router,
                RaceTestSupport.FakeUtcClock clock)
            {
                Controller = controller;
                Publisher = publisher;
                CountdownPublisher = countdownPublisher;
                Router = router;
                Clock = clock;
                signalBus.Subscribe<RaceSnapshotChangedSignal>(OnSignal);
                signalBus.Subscribe<RaceCountdownChangedSignal>(OnCountdownSignal);
            }

            public void ResetSignalCount()
            {
                SignalCount = 0;
                CountdownSignalCount = 0;
                LastSignal = default;
                LastCountdownSignal = default;
            }

            private void OnSignal(RaceSnapshotChangedSignal signal)
            {
                LastSignal = signal;
                SignalCount++;
            }

            private void OnCountdownSignal(RaceCountdownChangedSignal signal)
            {
                LastCountdownSignal = signal;
                CountdownSignalCount++;
            }
        }
    }
}
