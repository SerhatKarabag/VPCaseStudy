using NUnit.Framework;
using ThreadRace.App;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Infrastructure.Randomness;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceSimulationDriverTests
    {
        [Test]
        public void Driver_DoesNotAdvanceBeforeRaceStart()
        {
            var controller = CreateController(out var repository);
            var timeProvider = new RaceTestSupport.FakeRaceTimeProvider { UnscaledDeltaTime = 5f };
            var driver = new RaceSimulationDriver(controller, timeProvider);

            driver.Tick();

            Assert.AreEqual(RacePhase.NotStarted, controller.Phase);
            Assert.AreEqual(0, repository.SaveCount);
        }

        [Test]
        public void Driver_ForwardsUnscaledDeltaTimeWhileRunning()
        {
            var controller = CreateController(out var repository);
            var timeProvider = new RaceTestSupport.FakeRaceTimeProvider { UnscaledDeltaTime = 1f };
            var driver = new RaceSimulationDriver(controller, timeProvider);
            controller.StartNewRace();

            driver.Tick();

            Assert.AreEqual(2, repository.SaveCount);
            Assert.Greater(controller.CurrentSnapshot.Ranking[0].Progress, 0);
        }

        [Test]
        public void Driver_StopsAdvancingAfterCompletion()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository);
            var timeProvider = new RaceTestSupport.FakeRaceTimeProvider { UnscaledDeltaTime = 5f };
            var driver = new RaceSimulationDriver(controller, timeProvider);
            controller.StartNewRace();
            controller.ReportLevelResult(LevelResult.Success);
            var saveCountAfterCompletion = repository.SaveCount;
            var completedSignature = RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot);

            driver.Tick();

            Assert.AreEqual(saveCountAfterCompletion, repository.SaveCount);
            Assert.AreEqual(completedSignature, RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot));
        }

        private static RaceEventController CreateController(out RaceTestSupport.InMemoryRaceSaveRepository repository)
        {
            var settings = RaceTestSupport.CreateSettings();
            repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            return CreateController(settings, repository);
        }

        private static RaceEventController CreateController(
            ThreadRace.Gameplay.Config.RaceEventSettings settings,
            RaceTestSupport.InMemoryRaceSaveRepository repository)
        {
            return new RaceEventController(
                settings,
                repository,
                new RaceSaveDataMapper(),
                new SeededRandomSourceFactory(),
                new RaceTestSupport.FakeUtcClock());
        }
    }
}
