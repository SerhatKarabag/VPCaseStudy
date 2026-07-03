using NUnit.Framework;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Infrastructure.Randomness;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceDeterministicContinuationTests
    {
        [Test]
        public void RestoredSession_HasIdenticalSnapshotImmediately()
        {
            var settings = RaceTestSupport.CreateSettings(rangedAiDelays: true);
            var original = RaceTestSupport.CreateSession(settings);
            original.Start();
            original.AdvanceAi(0.35f);

            var restored = Restore(settings, original);

            Assert.AreEqual(
                RaceTestSupport.SnapshotSignature(original.GetSnapshot()),
                RaceTestSupport.SnapshotSignature(restored.GetSnapshot()));
        }

        [Test]
        public void RestoredSession_RemainsIdenticalAfterEqualFutureDeltaTimes()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 6, rangedAiDelays: true);
            var original = RaceTestSupport.CreateSession(settings);
            original.Start();
            original.AdvanceAi(0.35f);
            var restored = Restore(settings, original);

            var deltas = new[] { 0.2f, 0.4f, 0.9f, 1.1f, 0.6f };
            for (var i = 0; i < deltas.Length; i++)
            {
                if (original.Phase == RacePhase.Running)
                {
                    original.AdvanceAi(deltas[i]);
                }

                if (restored.Phase == RacePhase.Running)
                {
                    restored.AdvanceAi(deltas[i]);
                }
            }

            Assert.AreEqual(
                RaceTestSupport.SnapshotSignature(original.GetSnapshot()),
                RaceTestSupport.SnapshotSignature(restored.GetSnapshot()));
        }

        [Test]
        public void RestoredSession_RemainsIdenticalAfterEqualFuturePlayerCommands()
        {
            var settings = RaceTestSupport.CreateSettings(finishTarget: 5, rangedAiDelays: true);
            var original = RaceTestSupport.CreateSession(settings);
            original.Start();
            original.AdvanceAi(0.35f);
            var restored = Restore(settings, original);

            var commands = new[] { LevelResult.Success, LevelResult.Fail, LevelResult.Success };
            for (var i = 0; i < commands.Length; i++)
            {
                if (original.Phase == RacePhase.Running)
                {
                    original.ApplyPlayerResult(commands[i]);
                }

                if (restored.Phase == RacePhase.Running)
                {
                    restored.ApplyPlayerResult(commands[i]);
                }
            }

            Assert.AreEqual(
                RaceTestSupport.SnapshotSignature(original.GetSnapshot()),
                RaceTestSupport.SnapshotSignature(restored.GetSnapshot()));
        }

        [Test]
        public void RestoredRandomState_ProducesSameSubsequentSequence()
        {
            var random = new SeededRandomSource(98765);
            random.Range(0.25f, 0.75f);
            random.Range(1f, 2f);

            var restored = new SeededRandomSourceFactory().Restore(random.CurrentState);

            for (var i = 0; i < 8; i++)
            {
                Assert.AreEqual(random.Range(0.1f, 3f), restored.Range(0.1f, 3f));
            }
        }

        private static RaceSession Restore(ThreadRace.Gameplay.Config.RaceEventSettings settings, RaceSession original)
        {
            var mapper = new RaceSaveDataMapper();
            var timingState = original.Phase == RacePhase.NotStarted
                ? null
                : RaceTestSupport.CreateStartedTiming(settings);
            var saveData = mapper.Capture(settings, original, timingState);
            return mapper.Restore(settings, saveData, new SeededRandomSourceFactory());
        }
    }
}
