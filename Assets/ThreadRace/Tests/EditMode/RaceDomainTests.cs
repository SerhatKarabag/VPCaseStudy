using System;
using System.Text;
using NUnit.Framework;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Infrastructure.Randomness;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceDomainTests
    {
        [Test]
        public void ValidConfiguration_IsAccepted()
        {
            var configuration = CreateConfiguration();

            Assert.AreEqual(RaceConfiguration.RequiredRacerCount, configuration.Racers.Count);
            Assert.AreEqual(RaceConfiguration.DefaultFinishTarget, configuration.FinishTarget);
            Assert.AreEqual(RaceConfiguration.DefaultRewardedPositionCount, configuration.RewardedPositionCount);
            Assert.AreEqual(new RacerId(PlayerId), configuration.PlayerDefinition.Id);
        }

        [Test]
        public void NullConfigurationCollection_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RaceConfiguration(null, 10, 3));
        }

        [Test]
        public void DuplicateRacerIds_AreRejected()
        {
            var racers = CreateDefaultRacers();
            racers[2] = RacerDefinition.CreateAi(Ai1Id, "Duplicate", 2, 1f, 1f);

            Assert.Throws<ArgumentException>(() => new RaceConfiguration(racers, 10, 3));
        }

        [Test]
        public void EmptyRacerId_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RacerDefinition.CreatePlayer(string.Empty, "Player", 0));
        }

        [Test]
        public void EmptyDisplayName_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RacerDefinition.CreatePlayer(PlayerId, " ", 0));
        }

        [Test]
        public void MissingPlayer_IsRejected()
        {
            var racers = new[]
            {
                RacerDefinition.CreateAi("ai_0", "AI 0", 0, 1f, 1f),
                RacerDefinition.CreateAi(Ai1Id, "AI 1", 1, 1f, 1f),
                RacerDefinition.CreateAi(Ai2Id, "AI 2", 2, 1f, 1f),
                RacerDefinition.CreateAi(Ai3Id, "AI 3", 3, 1f, 1f),
                RacerDefinition.CreateAi(Ai4Id, "AI 4", 4, 1f, 1f)
            };

            Assert.Throws<ArgumentException>(() => new RaceConfiguration(racers, 10, 3));
        }

        [Test]
        public void MultiplePlayers_AreRejected()
        {
            var racers = new[]
            {
                RacerDefinition.CreatePlayer(PlayerId, "Player", 0),
                RacerDefinition.CreatePlayer("player_two", "Player Two", 1),
                RacerDefinition.CreateAi(Ai2Id, "AI 2", 2, 1f, 1f),
                RacerDefinition.CreateAi(Ai3Id, "AI 3", 3, 1f, 1f),
                RacerDefinition.CreateAi(Ai4Id, "AI 4", 4, 1f, 1f)
            };

            Assert.Throws<ArgumentException>(() => new RaceConfiguration(racers, 10, 3));
        }

        [Test]
        public void IncorrectRacerCount_IsRejected()
        {
            var racers = new[]
            {
                RacerDefinition.CreatePlayer(PlayerId, "Player", 0),
                RacerDefinition.CreateAi(Ai1Id, "AI 1", 1, 1f, 1f),
                RacerDefinition.CreateAi(Ai2Id, "AI 2", 2, 1f, 1f),
                RacerDefinition.CreateAi(Ai3Id, "AI 3", 3, 1f, 1f)
            };

            Assert.Throws<ArgumentException>(() => new RaceConfiguration(racers, 10, 3));
        }

        [Test]
        public void InvalidFinishAndRewardValues_AreRejected()
        {
            var racers = CreateDefaultRacers();

            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceConfiguration(racers, 0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceConfiguration(racers, 10, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceConfiguration(racers, 10, 6));
        }

        [Test]
        public void InvalidAiDelayRanges_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AiStepTiming(0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AiStepTiming(2f, 1f));
            Assert.Throws<ArgumentException>(() => new AiStepTiming(float.NaN, 1f));
        }

        [Test]
        public void RaceStartsInNotStarted()
        {
            var session = CreateSession();

            Assert.AreEqual(RacePhase.NotStarted, session.Phase);
            Assert.AreEqual(RacePhase.NotStarted, session.GetSnapshot().Phase);
        }

        [Test]
        public void Start_TransitionsToRunning()
        {
            var session = CreateSession();

            session.Start();

            Assert.AreEqual(RacePhase.Running, session.Phase);
            Assert.AreEqual(RacePhase.Running, session.GetSnapshot().Phase);
        }

        [Test]
        public void StartingAlreadyStarted_IsRejected()
        {
            var session = CreateSession();
            session.Start();

            Assert.Throws<InvalidOperationException>(() => session.Start());
        }

        [Test]
        public void PlayerSuccess_IncrementsProgressByExactlyOne()
        {
            var session = CreateSession();
            session.Start();

            session.ApplyPlayerResult(LevelResult.Success);

            Assert.AreEqual(1, FindRacer(session.GetSnapshot(), PlayerId).Progress);
        }

        [Test]
        public void PlayerFail_DoesNotChangeProgress()
        {
            var session = CreateSession();
            session.Start();

            session.ApplyPlayerResult(LevelResult.Fail);

            Assert.AreEqual(0, FindRacer(session.GetSnapshot(), PlayerId).Progress);
        }

        [Test]
        public void PlayerProgress_CannotExceedFinishTarget()
        {
            var session = CreateSession(CreateConfiguration(finishTarget: 2));
            session.Start();

            session.ApplyPlayerResult(LevelResult.Success);
            session.ApplyPlayerResult(LevelResult.Success);

            var player = FindRacer(session.GetSnapshot(), PlayerId);
            Assert.AreEqual(2, player.Progress);
            Assert.AreEqual(2, session.GetSnapshot().FinishTarget);
            Assert.Throws<InvalidOperationException>(() => session.ApplyPlayerResult(LevelResult.Success));
            Assert.AreEqual(2, FindRacer(session.GetSnapshot(), PlayerId).Progress);
        }

        [Test]
        public void AiRacers_AdvanceIndependently()
        {
            var session = CreateSession(CreateConfiguration(ai1Delay: 1f, ai2Delay: 2f, ai3Delay: 3f, ai4Delay: 4f));
            session.Start();

            session.AdvanceAi(1.5f);
            var firstSnapshot = session.GetSnapshot();
            Assert.AreEqual(1, FindRacer(firstSnapshot, Ai1Id).Progress);
            Assert.AreEqual(0, FindRacer(firstSnapshot, Ai2Id).Progress);

            session.AdvanceAi(0.6f);
            var secondSnapshot = session.GetSnapshot();
            Assert.AreEqual(2, FindRacer(secondSnapshot, Ai1Id).Progress);
            Assert.AreEqual(1, FindRacer(secondSnapshot, Ai2Id).Progress);
            Assert.AreEqual(0, FindRacer(secondSnapshot, Ai3Id).Progress);
            Assert.AreEqual(0, FindRacer(secondSnapshot, Ai4Id).Progress);
        }

        [Test]
        public void LargeDeltaTime_CanTriggerMultipleDeterministicAiSteps()
        {
            var session = CreateSession(CreateConfiguration(ai1Delay: 1f, ai2Delay: 1f, ai3Delay: 1f, ai4Delay: 1f));
            session.Start();

            session.AdvanceAi(3.5f);

            var snapshot = session.GetSnapshot();
            Assert.AreEqual(3, FindRacer(snapshot, Ai1Id).Progress);
            Assert.AreEqual(3, FindRacer(snapshot, Ai2Id).Progress);
            Assert.AreEqual(3, FindRacer(snapshot, Ai3Id).Progress);
            Assert.AreEqual(3, FindRacer(snapshot, Ai4Id).Progress);
        }

        [Test]
        public void AiRacers_StopProgressingAfterFinishing()
        {
            var session = CreateSession(CreateConfiguration(finishTarget: 1, rewardedPositions: 5, ai1Delay: 1f, ai2Delay: 1f, ai3Delay: 1f, ai4Delay: 1f));
            session.Start();

            session.AdvanceAi(1f);
            session.AdvanceAi(50f);

            var snapshot = session.GetSnapshot();
            Assert.AreEqual(RacePhase.Running, snapshot.Phase);
            Assert.AreEqual(1, FindRacer(snapshot, Ai1Id).Progress);
            Assert.AreEqual(1, FindRacer(snapshot, Ai2Id).Progress);
            Assert.AreEqual(1, FindRacer(snapshot, Ai3Id).Progress);
            Assert.AreEqual(1, FindRacer(snapshot, Ai4Id).Progress);
            Assert.IsTrue(FindRacer(snapshot, Ai1Id).IsFinished);
        }

        [Test]
        public void Ranking_ChangesAfterOvertake()
        {
            var session = CreateSession(CreateConfiguration(ai1Delay: 1f, ai2Delay: 10f, ai3Delay: 10f, ai4Delay: 10f));
            session.Start();

            Assert.AreEqual(PlayerId, session.GetSnapshot().Ranking[0].RacerId.Value);

            session.AdvanceAi(1f);

            var snapshot = session.GetSnapshot();
            Assert.AreEqual(Ai1Id, snapshot.Ranking[0].RacerId.Value);
            Assert.AreEqual(1, FindRacer(snapshot, Ai1Id).CurrentRank);
            Assert.AreEqual(2, FindRacer(snapshot, PlayerId).CurrentRank);
        }

        [Test]
        public void EqualProgress_UsesStableInitialOrder()
        {
            var session = CreateSession();

            var ranking = session.GetSnapshot().Ranking;

            Assert.AreEqual(PlayerId, ranking[0].RacerId.Value);
            Assert.AreEqual(Ai1Id, ranking[1].RacerId.Value);
            Assert.AreEqual(Ai2Id, ranking[2].RacerId.Value);
            Assert.AreEqual(Ai3Id, ranking[3].RacerId.Value);
            Assert.AreEqual(Ai4Id, ranking[4].RacerId.Value);
        }

        [Test]
        public void FinishOrder_IsRecordedOnlyOnce()
        {
            var session = CreateSession(CreateConfiguration(finishTarget: 1, rewardedPositions: 5, ai1Delay: 1f, ai2Delay: 100f, ai3Delay: 100f, ai4Delay: 100f));
            session.Start();

            session.AdvanceAi(1f);
            session.AdvanceAi(10f);

            var snapshot = session.GetSnapshot();
            var aiOne = FindRacer(snapshot, Ai1Id);
            Assert.AreEqual(1, aiOne.Progress);
            Assert.AreEqual(1, aiOne.FinishPlacement);
            Assert.AreEqual(1, CountFinisher(snapshot, Ai1Id));
        }

        [Test]
        public void PlayerFinishingFirst_IsRewardEligible()
        {
            var session = CreateSession(CreateConfiguration(finishTarget: 1));
            session.Start();

            session.ApplyPlayerResult(LevelResult.Success);

            var outcome = session.GetFinalOutcome();
            Assert.IsTrue(outcome.DidFinish);
            Assert.IsFalse(outcome.IsDnf);
            Assert.AreEqual(1, outcome.FinishPlacement);
            Assert.IsTrue(outcome.IsRewardEligible);
        }

        [Test]
        public void PlayerFinishingThird_IsRewardEligible()
        {
            var session = CreateSession(CreateConfiguration(finishTarget: 2, ai1Delay: 1f, ai2Delay: 1f, ai3Delay: 100f, ai4Delay: 100f));
            session.Start();
            session.AdvanceAi(2f);

            session.ApplyPlayerResult(LevelResult.Success);
            session.ApplyPlayerResult(LevelResult.Success);

            var outcome = session.GetFinalOutcome();
            Assert.IsTrue(outcome.DidFinish);
            Assert.AreEqual(3, outcome.FinishPlacement);
            Assert.IsTrue(outcome.IsRewardEligible);
        }

        [Test]
        public void ThreeAiRacersFinishingBeforePlayer_CompletesWithPlayerDnfAndNoReward()
        {
            var session = CreateSession(CreateConfiguration(finishTarget: 1, ai1Delay: 1f, ai2Delay: 1f, ai3Delay: 1f, ai4Delay: 100f));
            session.Start();

            session.AdvanceAi(1f);

            var snapshot = session.GetSnapshot();
            var outcome = session.GetFinalOutcome();
            Assert.AreEqual(RacePhase.Completed, snapshot.Phase);
            Assert.IsFalse(outcome.DidFinish);
            Assert.IsTrue(outcome.IsDnf);
            Assert.IsFalse(outcome.FinishPlacement.HasValue);
            Assert.IsFalse(outcome.IsRewardEligible);
            Assert.AreEqual(3, snapshot.Finishers.Count);
            Assert.AreEqual(0, FindRacer(snapshot, PlayerId).Progress);
        }

        [Test]
        public void NoMutationOccursAfterCompletion()
        {
            var session = CreateSession(CreateConfiguration(finishTarget: 1));
            session.Start();
            session.ApplyPlayerResult(LevelResult.Success);
            var completedSignature = SnapshotSignature(session.GetSnapshot());

            Assert.Throws<InvalidOperationException>(() => session.ApplyPlayerResult(LevelResult.Success));
            Assert.Throws<InvalidOperationException>(() => session.AdvanceAi(1f));

            Assert.AreEqual(completedSignature, SnapshotSignature(session.GetSnapshot()));
        }

        [Test]
        public void InvalidStateTransitions_AreRejected()
        {
            var session = CreateSession();

            Assert.Throws<InvalidOperationException>(() => session.ApplyPlayerResult(LevelResult.Success));
            Assert.Throws<InvalidOperationException>(() => session.AdvanceAi(1f));
            Assert.Throws<InvalidOperationException>(() => session.GetFinalOutcome());

            session.Start();

            Assert.Throws<InvalidOperationException>(() => session.Start());
        }

        [Test]
        public void NegativeDeltaTime_IsRejected()
        {
            var session = CreateSession();
            session.Start();

            Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceAi(-0.01f));
        }

        [Test]
        public void IdenticalDeterministicRandomSequences_ProduceIdenticalSnapshotsAndOutcomes()
        {
            var first = CreateSession(CreateConfigurationWithRandomDelayRanges(), new SeededRandomSource(12345));
            var second = CreateSession(CreateConfigurationWithRandomDelayRanges(), new SeededRandomSource(12345));

            first.Start();
            second.Start();

            for (var i = 0; i < 20; i++)
            {
                if (first.Phase == RacePhase.Running)
                {
                    first.AdvanceAi(0.5f);
                }

                if (second.Phase == RacePhase.Running)
                {
                    second.AdvanceAi(0.5f);
                }
            }

            Assert.AreEqual(SnapshotSignature(first.GetSnapshot()), SnapshotSignature(second.GetSnapshot()));
        }

        [Test]
        public void SeededRandomSource_WithSameSeed_ProducesSameSequence()
        {
            var first = new SeededRandomSource(9981);
            var second = new SeededRandomSource(9981);

            for (var i = 0; i < 16; i++)
            {
                Assert.AreEqual(first.Range(0.25f, 2.5f), second.Range(0.25f, 2.5f));
            }
        }

        [Test]
        public void SeededRandomSource_InvalidRange_IsRejected()
        {
            var random = new SeededRandomSource(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.Range(2f, 1f));
            Assert.Throws<ArgumentException>(() => random.Range(float.NaN, 1f));
        }

        private const string PlayerId = "player";
        private const string Ai1Id = "ai_1";
        private const string Ai2Id = "ai_2";
        private const string Ai3Id = "ai_3";
        private const string Ai4Id = "ai_4";

        private static RaceSession CreateSession()
        {
            return CreateSession(CreateConfiguration(), new SequenceRandomSource());
        }

        private static RaceSession CreateSession(RaceConfiguration configuration)
        {
            return CreateSession(configuration, new SequenceRandomSource());
        }

        private static RaceSession CreateSession(RaceConfiguration configuration, IDeterministicRandomSource randomSource)
        {
            return new RaceSession(configuration, randomSource);
        }

        private static RaceConfiguration CreateConfiguration(
            int finishTarget = RaceConfiguration.DefaultFinishTarget,
            int rewardedPositions = RaceConfiguration.DefaultRewardedPositionCount,
            float ai1Delay = 1f,
            float ai2Delay = 1f,
            float ai3Delay = 1f,
            float ai4Delay = 1f)
        {
            return new RaceConfiguration(
                new[]
                {
                    RacerDefinition.CreatePlayer(PlayerId, "Player", 0),
                    RacerDefinition.CreateAi(Ai1Id, "AI 1", 1, ai1Delay, ai1Delay),
                    RacerDefinition.CreateAi(Ai2Id, "AI 2", 2, ai2Delay, ai2Delay),
                    RacerDefinition.CreateAi(Ai3Id, "AI 3", 3, ai3Delay, ai3Delay),
                    RacerDefinition.CreateAi(Ai4Id, "AI 4", 4, ai4Delay, ai4Delay)
                },
                finishTarget,
                rewardedPositions);
        }

        private static RaceConfiguration CreateConfigurationWithRandomDelayRanges()
        {
            return new RaceConfiguration(
                new[]
                {
                    RacerDefinition.CreatePlayer(PlayerId, "Player", 0),
                    RacerDefinition.CreateAi(Ai1Id, "AI 1", 1, 0.25f, 0.75f),
                    RacerDefinition.CreateAi(Ai2Id, "AI 2", 2, 0.4f, 0.9f),
                    RacerDefinition.CreateAi(Ai3Id, "AI 3", 3, 0.5f, 1.1f),
                    RacerDefinition.CreateAi(Ai4Id, "AI 4", 4, 0.6f, 1.2f)
                },
                5,
                3);
        }

        private static RacerDefinition[] CreateDefaultRacers()
        {
            return new[]
            {
                RacerDefinition.CreatePlayer(PlayerId, "Player", 0),
                RacerDefinition.CreateAi(Ai1Id, "AI 1", 1, 1f, 1f),
                RacerDefinition.CreateAi(Ai2Id, "AI 2", 2, 1f, 1f),
                RacerDefinition.CreateAi(Ai3Id, "AI 3", 3, 1f, 1f),
                RacerDefinition.CreateAi(Ai4Id, "AI 4", 4, 1f, 1f)
            };
        }

        private static RacerSnapshot FindRacer(RaceSnapshot snapshot, string racerId)
        {
            for (var i = 0; i < snapshot.Racers.Count; i++)
            {
                if (snapshot.Racers[i].Id.Value == racerId)
                {
                    return snapshot.Racers[i];
                }
            }

            Assert.Fail($"Racer '{racerId}' was not found in the snapshot.");
            return null;
        }

        private static int CountFinisher(RaceSnapshot snapshot, string racerId)
        {
            var count = 0;
            for (var i = 0; i < snapshot.Finishers.Count; i++)
            {
                if (snapshot.Finishers[i].RacerId.Value == racerId)
                {
                    count++;
                }
            }

            return count;
        }

        private static string SnapshotSignature(RaceSnapshot snapshot)
        {
            var builder = new StringBuilder();
            builder.Append(snapshot.Phase);
            builder.Append('|');
            builder.Append(snapshot.FinishTarget);
            builder.Append('|');
            builder.Append(snapshot.RewardedPositionCount);
            builder.Append('|');

            for (var i = 0; i < snapshot.Racers.Count; i++)
            {
                var racer = snapshot.Racers[i];
                builder.Append(racer.Id.Value);
                builder.Append(':');
                builder.Append(racer.Progress);
                builder.Append(':');
                builder.Append(racer.CurrentRank);
                builder.Append(':');
                builder.Append(racer.IsFinished);
                builder.Append(':');
                builder.Append(racer.FinishPlacement.HasValue ? racer.FinishPlacement.Value : 0);
                builder.Append(';');
            }

            builder.Append('|');
            for (var i = 0; i < snapshot.Finishers.Count; i++)
            {
                builder.Append(snapshot.Finishers[i].RacerId.Value);
                builder.Append('#');
                builder.Append(snapshot.Finishers[i].FinishPlacement);
                builder.Append(';');
            }

            if (snapshot.PlayerOutcome != null)
            {
                builder.Append('|');
                builder.Append(snapshot.PlayerOutcome.DidFinish);
                builder.Append(':');
                builder.Append(snapshot.PlayerOutcome.IsDnf);
                builder.Append(':');
                builder.Append(snapshot.PlayerOutcome.FinishPlacement.HasValue ? snapshot.PlayerOutcome.FinishPlacement.Value : 0);
                builder.Append(':');
                builder.Append(snapshot.PlayerOutcome.IsRewardEligible);
            }

            return builder.ToString();
        }

        private sealed class SequenceRandomSource : IDeterministicRandomSource
        {
            public int Seed => 0;

            public DeterministicRandomState CurrentState => new DeterministicRandomState(Seed, 0, 0);

            public float Range(float minInclusive, float maxInclusive)
            {
                if (minInclusive > maxInclusive)
                {
                    throw new ArgumentOutOfRangeException(nameof(minInclusive));
                }

                return minInclusive;
            }
        }
    }
}
