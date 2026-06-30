using System.Text;
using ThreadRace.Core.Time;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;
using ThreadRace.Infrastructure.Randomness;

namespace ThreadRace.Tests.EditMode
{
    internal static class RaceTestSupport
    {
        public const string PlayerId = "player";
        public const string Ai1Id = "ai_01";
        public const string Ai2Id = "ai_02";
        public const string Ai3Id = "ai_03";
        public const string Ai4Id = "ai_04";
        public const string SaveKey = "ThreadRace.Tests.Save";
        public const int SaveVersion = 1;
        public const int DefaultSeed = 123456;

        public static RaceEventSettings CreateSettings(
            int finishTarget = RaceConfiguration.DefaultFinishTarget,
            int rewardedPositions = RaceConfiguration.DefaultRewardedPositionCount,
            bool rangedAiDelays = false)
        {
            return new RaceEventSettings(
                CreateConfiguration(finishTarget, rewardedPositions, rangedAiDelays),
                SaveVersion,
                SaveKey,
                DefaultSeed);
        }

        public static RaceConfiguration CreateConfiguration(
            int finishTarget = RaceConfiguration.DefaultFinishTarget,
            int rewardedPositions = RaceConfiguration.DefaultRewardedPositionCount,
            bool rangedAiDelays = false)
        {
            if (rangedAiDelays)
            {
                return new RaceConfiguration(
                    new[]
                    {
                        RacerDefinition.CreatePlayer(PlayerId, "Player", 0),
                        RacerDefinition.CreateAi(Ai1Id, "Nova", 1, 0.45f, 0.85f),
                        RacerDefinition.CreateAi(Ai2Id, "Bolt", 2, 0.55f, 0.95f),
                        RacerDefinition.CreateAi(Ai3Id, "Mina", 3, 0.65f, 1.05f),
                        RacerDefinition.CreateAi(Ai4Id, "Rex", 4, 0.75f, 1.15f)
                    },
                    finishTarget,
                    rewardedPositions);
            }

            return new RaceConfiguration(
                new[]
                {
                    RacerDefinition.CreatePlayer(PlayerId, "Player", 0),
                    RacerDefinition.CreateAi(Ai1Id, "Nova", 1, 1f, 1f),
                    RacerDefinition.CreateAi(Ai2Id, "Bolt", 2, 1f, 1f),
                    RacerDefinition.CreateAi(Ai3Id, "Mina", 3, 1f, 1f),
                    RacerDefinition.CreateAi(Ai4Id, "Rex", 4, 1f, 1f)
                },
                finishTarget,
                rewardedPositions);
        }

        public static RaceSession CreateSession(RaceEventSettings settings)
        {
            return new RaceSession(settings.RaceConfiguration, new SeededRandomSource(settings.DefaultSeed));
        }

        public static RaceSaveData CaptureStartedSave(RaceEventSettings settings)
        {
            var session = CreateSession(settings);
            session.Start();
            return new RaceSaveDataMapper().Capture(settings, session);
        }

        public static RaceSaveData CaptureCompletedRewardedSave()
        {
            var settings = CreateSettings(finishTarget: 1);
            var session = CreateSession(settings);
            session.Start();
            session.ApplyPlayerResult(LevelResult.Success);
            return new RaceSaveDataMapper().Capture(settings, session);
        }

        public static RaceSaveData CaptureCompletedDnfSave()
        {
            var settings = CreateSettings(finishTarget: 1);
            var session = CreateSession(settings);
            session.Start();
            session.AdvanceAi(1f);
            return new RaceSaveDataMapper().Capture(settings, session);
        }

        public static string SnapshotSignature(RaceSnapshot snapshot)
        {
            var builder = new StringBuilder();
            builder.Append(snapshot.Phase).Append('|');
            builder.Append(snapshot.FinishTarget).Append('|');
            builder.Append(snapshot.RewardedPositionCount).Append('|');

            for (var i = 0; i < snapshot.Racers.Count; i++)
            {
                var racer = snapshot.Racers[i];
                builder.Append(racer.Id.Value).Append(':');
                builder.Append(racer.Progress).Append(':');
                builder.Append(racer.CurrentRank).Append(':');
                builder.Append(racer.IsFinished).Append(':');
                builder.Append(racer.FinishPlacement.GetValueOrDefault()).Append(';');
            }

            builder.Append('|');
            for (var i = 0; i < snapshot.Finishers.Count; i++)
            {
                builder.Append(snapshot.Finishers[i].RacerId.Value).Append('#');
                builder.Append(snapshot.Finishers[i].FinishPlacement).Append(';');
            }

            if (snapshot.PlayerOutcome != null)
            {
                builder.Append('|');
                builder.Append(snapshot.PlayerOutcome.DidFinish).Append(':');
                builder.Append(snapshot.PlayerOutcome.IsDnf).Append(':');
                builder.Append(snapshot.PlayerOutcome.FinishPlacement.GetValueOrDefault()).Append(':');
                builder.Append(snapshot.PlayerOutcome.IsRewardEligible);
            }

            return builder.ToString();
        }

        public static string SaveSignature(RaceSaveData saveData)
        {
            var builder = new StringBuilder();
            builder.Append(saveData.SchemaVersion).Append('|');
            builder.Append(saveData.Phase).Append('|');
            builder.Append(saveData.Revision).Append('|');
            builder.Append(saveData.RandomState.Seed).Append(':');
            builder.Append(saveData.RandomState.State).Append(':');
            builder.Append(saveData.RandomState.ConsumedCount).Append('|');

            for (var i = 0; i < saveData.Racers.Count; i++)
            {
                var racer = saveData.Racers[i];
                builder.Append(racer.RacerId.Value).Append(':');
                builder.Append(racer.Progress).Append(':');
                builder.Append(racer.IsFinished).Append(':');
                builder.Append(racer.FinishPlacement.GetValueOrDefault()).Append(':');
                builder.Append(racer.AiStepTimeRemaining.HasValue ? racer.AiStepTimeRemaining.Value.ToString("R") : "null").Append(';');
            }

            builder.Append('|');
            for (var i = 0; i < saveData.FinishOrder.Count; i++)
            {
                builder.Append(saveData.FinishOrder[i].Value).Append(';');
            }

            if (saveData.PlayerOutcome != null)
            {
                builder.Append('|');
                builder.Append(saveData.PlayerOutcome.PlayerId.Value).Append(':');
                builder.Append(saveData.PlayerOutcome.DidFinish).Append(':');
                builder.Append(saveData.PlayerOutcome.IsDnf).Append(':');
                builder.Append(saveData.PlayerOutcome.FinishPlacement.GetValueOrDefault()).Append(':');
                builder.Append(saveData.PlayerOutcome.IsRewardEligible);
            }

            return builder.ToString();
        }

        public sealed class InMemoryRaceSaveRepository : IRaceSaveRepository
        {
            private RaceSaveLoadResult _loadResult;

            public InMemoryRaceSaveRepository()
                : this(RaceSaveLoadResult.NotFound())
            {
            }

            public InMemoryRaceSaveRepository(RaceSaveLoadResult loadResult)
            {
                _loadResult = loadResult;
            }

            public int SaveCount { get; private set; }

            public int ClearCount { get; private set; }

            public string LastSaveKey { get; private set; }

            public RaceSaveData LastSavedData { get; private set; }

            public RaceSaveLoadResult Load(string saveKey)
            {
                LastSaveKey = saveKey;
                return _loadResult;
            }

            public void Save(string saveKey, RaceSaveData saveData)
            {
                SaveCount++;
                LastSaveKey = saveKey;
                LastSavedData = saveData;
                _loadResult = RaceSaveLoadResult.Loaded(saveData);
            }

            public void Clear(string saveKey)
            {
                ClearCount++;
                LastSaveKey = saveKey;
                LastSavedData = null;
                _loadResult = RaceSaveLoadResult.NotFound();
            }
        }

        public sealed class FakeRaceTimeProvider : IRaceTimeProvider
        {
            public float UnscaledDeltaTime { get; set; }
        }
    }
}
