using System;
using NUnit.Framework;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Infrastructure.Config;
using UnityEngine;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceEventConfigAssetTests
    {
        [Test]
        public void ValidAuthoringData_ConvertsToRuntimeSettings()
        {
            var asset = CreateValidAsset();

            var settings = asset.ToRuntimeSettings();

            Assert.AreEqual(RaceTestSupport.SaveVersion, settings.SaveSchemaVersion);
            Assert.AreEqual(RaceTestSupport.SaveKey, settings.SaveKey);
            Assert.AreEqual(RaceTestSupport.DefaultSeed, settings.DefaultSeed);
            Assert.AreEqual(RaceEventSettings.DefaultEventDurationSeconds, settings.EventDurationSeconds);
            Assert.AreEqual(RaceEventSettings.DefaultCountdownUpdateIntervalSeconds, settings.CountdownUpdateIntervalSeconds);
            Assert.AreEqual(RaceConfiguration.DefaultFinishTarget, settings.RaceConfiguration.FinishTarget);
            Assert.AreEqual(RaceConfiguration.DefaultRewardedPositionCount, settings.RaceConfiguration.RewardedPositionCount);
            Assert.AreEqual(3, settings.RaceConfiguration.RewardTiers.Count);
            Assert.AreEqual(1, settings.RaceConfiguration.RewardTiers[0].Rank);
            Assert.AreEqual("thread_race_rank_1_coins", settings.RaceConfiguration.RewardTiers[0].RewardId);
            Assert.AreEqual(RewardType.Coins, settings.RaceConfiguration.RewardTiers[0].RewardType);
            Assert.AreEqual(1000, settings.RaceConfiguration.RewardTiers[0].Amount);
            Assert.AreEqual("1000 Coins", settings.RaceConfiguration.RewardTiers[0].DisplayText);
            Assert.AreEqual("coin_stack", settings.RaceConfiguration.RewardTiers[0].IconId);
            Assert.AreEqual(RaceConfiguration.DefaultRacerCount, settings.RaceConfiguration.Racers.Count);
            Assert.AreEqual(new RacerId(RaceTestSupport.PlayerId), settings.RaceConfiguration.PlayerDefinition.Id);
            var boltProfile = settings.RaceConfiguration.GetRacer(2).AiPacingProfile;
            Assert.AreEqual(AiPacingStyle.Sprinter, boltProfile.Style);
            Assert.IsTrue(boltProfile.UsesDynamicPlanning);
            Assert.AreEqual(0.52f, boltProfile.Skill);
            Assert.AreEqual(0.64f, boltProfile.Consistency);
            Assert.AreEqual(0.38f, boltProfile.Volatility);
            Assert.AreEqual(0.62f, boltProfile.EarlyPaceBias);
            Assert.AreEqual(-0.18f, boltProfile.LatePaceBias);
            Assert.AreEqual(0.13f, boltProfile.BurstChance);
            Assert.AreEqual(0.08f, boltProfile.SlumpChance);
            Assert.AreEqual(0.04f, boltProfile.FinalPushChance);
        }

        [Test]
        public void MissingAiRacer_IsRejected()
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                RaceEventSettings.DefaultEventDurationSeconds,
                RaceEventSettings.DefaultCountdownUpdateIntervalSeconds,
                10,
                1,
                new[]
                {
                    new RaceEventConfigAsset.RewardTierAuthoringData(
                        1,
                        "thread_race_rank_1_coins",
                        RewardType.Coins,
                        1000,
                        "1000 Coins",
                        "coin_stack")
                },
                new[]
                {
                    Player()
                });

            Assert.Throws<ArgumentException>(() => asset.ToRuntimeSettings());
        }

        [Test]
        public void VariableRacerCount_ConvertsFromAuthoringData()
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                RaceEventSettings.DefaultEventDurationSeconds,
                RaceEventSettings.DefaultCountdownUpdateIntervalSeconds,
                12,
                4,
                new[]
                {
                    new RaceEventConfigAsset.RewardTierAuthoringData(1, "rank_1", RewardType.Coins, 1000, "1000 Coins", "coin_stack"),
                    new RaceEventConfigAsset.RewardTierAuthoringData(2, "rank_2", RewardType.Coins, 500, "500 Coins", "coin_stack"),
                    new RaceEventConfigAsset.RewardTierAuthoringData(3, "rank_3", RewardType.Coins, 250, "250 Coins", "coin_stack"),
                    new RaceEventConfigAsset.RewardTierAuthoringData(4, "rank_4", RewardType.Coins, 100, "100 Coins", "coin_stack")
                },
                new[]
                {
                    Player(),
                    Ai(RaceTestSupport.Ai1Id, "Nova"),
                    Ai(RaceTestSupport.Ai2Id, "Bolt"),
                    Ai(RaceTestSupport.Ai3Id, "Mina"),
                    Ai(RaceTestSupport.Ai4Id, "Rex"),
                    Ai("ai_05", "Ivy")
                });

            var settings = asset.ToRuntimeSettings();

            Assert.AreEqual(6, settings.RaceConfiguration.Racers.Count);
            Assert.AreEqual(12, settings.RaceConfiguration.FinishTarget);
            Assert.AreEqual(4, settings.RaceConfiguration.RewardedPositionCount);
            Assert.AreEqual("rank_4", settings.RaceConfiguration.GetRewardTierForRank(4).RewardId);
        }

        [Test]
        public void InvalidSaveKeyOrVersion_IsRejected()
        {
            var invalidVersion = CreateValidAsset(saveVersion: 0);
            var invalidKey = CreateValidAsset(saveKey: " ");

            Assert.Throws<ArgumentOutOfRangeException>(() => invalidVersion.ToRuntimeSettings());
            Assert.Throws<ArgumentException>(() => invalidKey.ToRuntimeSettings());
        }

        [Test]
        public void InvalidDurationOrCountdownInterval_IsRejected()
        {
            var zeroDuration = CreateValidAsset(eventDurationSeconds: 0);
            var negativeDuration = CreateValidAsset(eventDurationSeconds: -1);
            var invalidCountdownInterval = CreateValidAsset(countdownUpdateIntervalSeconds: 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => zeroDuration.ToRuntimeSettings());
            Assert.Throws<ArgumentOutOfRangeException>(() => negativeDuration.ToRuntimeSettings());
            Assert.Throws<ArgumentOutOfRangeException>(() => invalidCountdownInterval.ToRuntimeSettings());
        }

        [Test]
        public void RewardTierMissingIconId_IsRejected()
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                RaceEventSettings.DefaultEventDurationSeconds,
                RaceEventSettings.DefaultCountdownUpdateIntervalSeconds,
                10,
                3,
                new[]
                {
                    new RaceEventConfigAsset.RewardTierAuthoringData(
                        1,
                        "thread_race_rank_1_coins",
                        RewardType.Coins,
                        1000,
                        "1000 Coins",
                        " ")
                },
                DefaultRacers());

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToRuntimeSettings());
            StringAssert.Contains("requires an icon ID", exception.Message);
        }

        [Test]
        public void MissingRewardTiers_AreRejected()
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                RaceEventSettings.DefaultEventDurationSeconds,
                RaceEventSettings.DefaultCountdownUpdateIntervalSeconds,
                10,
                3,
                Array.Empty<RaceEventConfigAsset.RewardTierAuthoringData>(),
                DefaultRacers());

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToRuntimeSettings());
            StringAssert.Contains("requires at least one authored reward tier", exception.Message);
        }

        [Test]
        public void RewardedPositionCountMustMatchRewardTierCount()
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                RaceEventSettings.DefaultEventDurationSeconds,
                RaceEventSettings.DefaultCountdownUpdateIntervalSeconds,
                10,
                3,
                new[]
                {
                    new RaceEventConfigAsset.RewardTierAuthoringData(
                        1,
                        "thread_race_rank_1_coins",
                        RewardType.Coins,
                        1000,
                        "1000 Coins",
                        "coin_stack")
                },
                DefaultRacers());

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToRuntimeSettings());
            StringAssert.Contains("must match authored reward tier count", exception.Message);
        }

        [Test]
        public void Conversion_DoesNotExposeSerializedMutableList()
        {
            var authoringRacers = DefaultRacers();
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                RaceEventSettings.DefaultEventDurationSeconds,
                RaceEventSettings.DefaultCountdownUpdateIntervalSeconds,
                10,
                3,
                authoringRacers);

            var settings = asset.ToRuntimeSettings();
            authoringRacers[0] = Ai("mutated", "Mutated");
            asset.Configure(
                0,
                " ",
                0,
                0,
                0,
                0,
                0,
                Array.Empty<RaceEventConfigAsset.RewardTierAuthoringData>(),
                Array.Empty<RaceEventConfigAsset.RacerAuthoringData>());

            Assert.AreEqual(RaceConfiguration.DefaultRacerCount, settings.RaceConfiguration.Racers.Count);
            Assert.AreEqual(new RacerId(RaceTestSupport.PlayerId), settings.RaceConfiguration.Racers[0].Id);
            Assert.AreEqual(10, settings.RaceConfiguration.FinishTarget);
        }

        private static RaceEventConfigAsset CreateValidAsset(
            int saveVersion = RaceTestSupport.SaveVersion,
            string saveKey = RaceTestSupport.SaveKey,
            long eventDurationSeconds = RaceEventSettings.DefaultEventDurationSeconds,
            int countdownUpdateIntervalSeconds = RaceEventSettings.DefaultCountdownUpdateIntervalSeconds)
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                saveVersion,
                saveKey,
                RaceTestSupport.DefaultSeed,
                eventDurationSeconds,
                countdownUpdateIntervalSeconds,
                RaceConfiguration.DefaultFinishTarget,
                RaceConfiguration.DefaultRewardedPositionCount,
                DefaultRewardTiers(),
                DefaultRacers());
            return asset;
        }

        private static RaceEventConfigAsset.RacerAuthoringData[] DefaultRacers()
        {
            return new[]
            {
                Player(),
                Ai(RaceTestSupport.Ai1Id, "Nova", 1.1f, 1.8f, AiPacingStyle.Steady, true, 0.52f, 0.9f, 0.16f, 0.02f, 0.08f, 0.04f, 0.03f, 0.08f),
                Ai(RaceTestSupport.Ai2Id, "Bolt", 1.2f, 1.9f, AiPacingStyle.Sprinter, true, 0.52f, 0.64f, 0.38f, 0.62f, -0.18f, 0.13f, 0.08f, 0.04f),
                Ai(RaceTestSupport.Ai3Id, "Mina", 1.3f, 2.1f, AiPacingStyle.Closer, true, 0.52f, 0.72f, 0.3f, -0.18f, 0.64f, 0.08f, 0.07f, 0.2f),
                Ai(RaceTestSupport.Ai4Id, "Rex", 1.4f, 2.2f, AiPacingStyle.Wildcard, true, 0.51f, 0.45f, 0.76f, 0.04f, 0.16f, 0.22f, 0.16f, 0.14f)
            };
        }

        private static RaceEventConfigAsset.RewardTierAuthoringData[] DefaultRewardTiers()
        {
            return new[]
            {
                new RaceEventConfigAsset.RewardTierAuthoringData(
                    1,
                    "thread_race_rank_1_coins",
                    RewardType.Coins,
                    1000,
                    "1000 Coins",
                    "coin_stack"),
                new RaceEventConfigAsset.RewardTierAuthoringData(
                    2,
                    "thread_race_rank_2_coins",
                    RewardType.Coins,
                    500,
                    "500 Coins",
                    "coin_stack"),
                new RaceEventConfigAsset.RewardTierAuthoringData(
                    3,
                    "thread_race_rank_3_coins",
                    RewardType.Coins,
                    250,
                    "250 Coins",
                    "coin_stack")
            };
        }

        private static RaceEventConfigAsset.RacerAuthoringData Player()
        {
            return new RaceEventConfigAsset.RacerAuthoringData(
                RaceTestSupport.PlayerId,
                "Player",
                RacerType.Player,
                0f,
                0f);
        }

        private static RaceEventConfigAsset.RacerAuthoringData Ai(
            string id,
            string name,
            float minimumDelay = 1f,
            float maximumDelay = 1f,
            AiPacingStyle pacingStyle = AiPacingStyle.LegacyFixed,
            bool usesDynamicPlanning = false,
            float skill = 0.5f,
            float consistency = 1f,
            float volatility = 0f,
            float earlyPaceBias = 0f,
            float latePaceBias = 0f,
            float burstChance = 0f,
            float slumpChance = 0f,
            float finalPushChance = 0f)
        {
            return new RaceEventConfigAsset.RacerAuthoringData(
                id,
                name,
                RacerType.Ai,
                minimumDelay,
                maximumDelay,
                pacingStyle,
                usesDynamicPlanning,
                skill,
                consistency,
                volatility,
                earlyPaceBias,
                latePaceBias,
                burstChance,
                slumpChance,
                finalPushChance);
        }
    }
}
