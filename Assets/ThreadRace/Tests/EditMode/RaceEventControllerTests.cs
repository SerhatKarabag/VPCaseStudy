using NUnit.Framework;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;
using ThreadRace.Infrastructure.Randomness;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceEventControllerTests
    {
        [Test]
        public void NoSaveInitialization_RemainsReadyForNewRace()
        {
            var controller = CreateController(out _);

            Assert.AreEqual(RaceControllerInitializationStatus.NoSave, controller.InitializationResult.Status);
            Assert.AreEqual(RacePhase.NotStarted, controller.Phase);
        }

        [Test]
        public void ValidSaveInitialization_RestoresRace()
        {
            var settings = RaceTestSupport.CreateSettings(rangedAiDelays: true);
            var saveData = RaceTestSupport.CaptureStartedSave(settings);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository(RaceSaveLoadResult.Loaded(saveData));

            var controller = CreateController(settings, repository);

            Assert.AreEqual(RaceControllerInitializationStatus.Restored, controller.InitializationResult.Status);
            Assert.AreEqual(RacePhase.Running, controller.Phase);
            Assert.AreEqual(
                RaceTestSupport.SnapshotSignature(new RaceSaveDataMapper().Restore(settings, saveData, new SeededRandomSourceFactory()).GetSnapshot()),
                RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot));
        }

        [Test]
        public void InvalidSaveInitialization_IsSurfacedExplicitly()
        {
            var settings = RaceTestSupport.CreateSettings();
            var invalidSave = RaceTestSupport.CaptureStartedSave(settings);
            invalidSave = new RaceSaveData(
                99,
                invalidSave.Phase,
                invalidSave.Racers,
                invalidSave.FinishOrder,
                invalidSave.PlayerOutcome,
                invalidSave.RandomState,
                invalidSave.Revision);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository(RaceSaveLoadResult.Loaded(invalidSave));

            var controller = CreateController(settings, repository);

            Assert.AreEqual(RaceControllerInitializationStatus.InvalidSave, controller.InitializationResult.Status);
            Assert.AreEqual(RacePhase.NotStarted, controller.Phase);
            Assert.AreEqual(0, repository.SaveCount);
        }

        [Test]
        public void StartingRace_WritesSave()
        {
            var controller = CreateController(out var repository);

            Assert.IsTrue(controller.StartNewRace());

            Assert.AreEqual(1, repository.SaveCount);
            Assert.AreEqual(RacePhase.Running, repository.LastSavedData.Phase);
        }

        [Test]
        public void PlayerSuccess_WritesWhenStateChanges()
        {
            var controller = CreateController(out var repository);
            controller.StartNewRace();

            Assert.IsTrue(controller.ReportLevelResult(LevelResult.Success));

            Assert.AreEqual(2, repository.SaveCount);
        }

        [Test]
        public void PlayerFail_AvoidsUnnecessaryWrite()
        {
            var controller = CreateController(out var repository);
            controller.StartNewRace();

            Assert.IsFalse(controller.ReportLevelResult(LevelResult.Fail));

            Assert.AreEqual(1, repository.SaveCount);
        }

        [Test]
        public void AiTick_WritesOnlyWhenAiProgressChanges()
        {
            var settings = RaceTestSupport.CreateSettings();
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository);
            controller.StartNewRace();

            Assert.IsFalse(controller.AdvanceAi(0.5f));
            Assert.AreEqual(1, repository.SaveCount);

            Assert.IsTrue(controller.AdvanceAi(0.5f));
            Assert.AreEqual(2, repository.SaveCount);
        }

        [Test]
        public void CompletedRace_RemainsRestoredAsCompleted()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository(
                RaceSaveLoadResult.Loaded(RaceTestSupport.CaptureCompletedRewardedSave()));

            var controller = CreateController(settings, repository);

            Assert.AreEqual(RaceControllerInitializationStatus.Restored, controller.InitializationResult.Status);
            Assert.AreEqual(RacePhase.Completed, controller.Phase);
        }

        [Test]
        public void Reset_ClearsPersistenceAndReturnsToNotStarted()
        {
            var controller = CreateController(out var repository);
            controller.StartNewRace();

            controller.Reset();

            Assert.AreEqual(1, repository.ClearCount);
            Assert.AreEqual(RacePhase.NotStarted, controller.Phase);
            Assert.AreEqual(RaceControllerInitializationStatus.Reset, controller.InitializationResult.Status);
        }

        [Test]
        public void NoControllerMutationOccursAfterCompletion()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 1);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository();
            var controller = CreateController(settings, repository);
            controller.StartNewRace();
            controller.ReportLevelResult(LevelResult.Success);
            var saveCountAfterCompletion = repository.SaveCount;
            var completedSignature = RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot);

            Assert.IsFalse(controller.ReportLevelResult(LevelResult.Fail));
            Assert.IsFalse(controller.AdvanceAi(1f));

            Assert.AreEqual(saveCountAfterCompletion, repository.SaveCount);
            Assert.AreEqual(completedSignature, RaceTestSupport.SnapshotSignature(controller.CurrentSnapshot));
        }

        [Test]
        public void InvalidSave_BlocksImplicitStartUntilReset()
        {
            var settings = RaceTestSupport.CreateSettings();
            var invalidSave = new RaceSaveData(
                99,
                RacePhase.NotStarted,
                RaceTestSupport.CaptureStartedSave(settings).Racers,
                new RacerId[0],
                null,
                RaceTestSupport.CaptureStartedSave(settings).RandomState,
                0);
            var repository = new RaceTestSupport.InMemoryRaceSaveRepository(RaceSaveLoadResult.Loaded(invalidSave));
            var controller = CreateController(settings, repository);

            Assert.IsFalse(controller.StartNewRace());

            controller.Reset();

            Assert.IsTrue(controller.StartNewRace());
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
                new SeededRandomSourceFactory());
        }
    }
}
