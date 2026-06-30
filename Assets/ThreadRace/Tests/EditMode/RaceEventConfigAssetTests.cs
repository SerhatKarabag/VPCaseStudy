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
            Assert.AreEqual(RaceConfiguration.DefaultFinishTarget, settings.RaceConfiguration.FinishTarget);
            Assert.AreEqual(RaceConfiguration.DefaultRewardedPositionCount, settings.RaceConfiguration.RewardedPositionCount);
            Assert.AreEqual(RaceConfiguration.RequiredRacerCount, settings.RaceConfiguration.Racers.Count);
            Assert.AreEqual(new RacerId(RaceTestSupport.PlayerId), settings.RaceConfiguration.PlayerDefinition.Id);
        }

        [Test]
        public void InvalidRacerCount_IsRejected()
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                10,
                3,
                new[]
                {
                    Player(),
                    Ai(RaceTestSupport.Ai1Id, "Nova"),
                    Ai(RaceTestSupport.Ai2Id, "Bolt"),
                    Ai(RaceTestSupport.Ai3Id, "Mina")
                });

            Assert.Throws<ArgumentException>(() => asset.ToRuntimeSettings());
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
        public void Conversion_DoesNotExposeSerializedMutableList()
        {
            var authoringRacers = DefaultRacers();
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                RaceTestSupport.SaveVersion,
                RaceTestSupport.SaveKey,
                RaceTestSupport.DefaultSeed,
                10,
                3,
                authoringRacers);

            var settings = asset.ToRuntimeSettings();
            authoringRacers[0] = Ai("mutated", "Mutated");
            asset.Configure(0, " ", 0, 0, 0, Array.Empty<RaceEventConfigAsset.RacerAuthoringData>());

            Assert.AreEqual(RaceConfiguration.RequiredRacerCount, settings.RaceConfiguration.Racers.Count);
            Assert.AreEqual(new RacerId(RaceTestSupport.PlayerId), settings.RaceConfiguration.Racers[0].Id);
            Assert.AreEqual(10, settings.RaceConfiguration.FinishTarget);
        }

        private static RaceEventConfigAsset CreateValidAsset(
            int saveVersion = RaceTestSupport.SaveVersion,
            string saveKey = RaceTestSupport.SaveKey)
        {
            var asset = ScriptableObject.CreateInstance<RaceEventConfigAsset>();
            asset.Configure(
                saveVersion,
                saveKey,
                RaceTestSupport.DefaultSeed,
                RaceConfiguration.DefaultFinishTarget,
                RaceConfiguration.DefaultRewardedPositionCount,
                DefaultRacers());
            return asset;
        }

        private static RaceEventConfigAsset.RacerAuthoringData[] DefaultRacers()
        {
            return new[]
            {
                Player(),
                Ai(RaceTestSupport.Ai1Id, "Nova", 1.1f, 1.8f),
                Ai(RaceTestSupport.Ai2Id, "Bolt", 1.2f, 1.9f),
                Ai(RaceTestSupport.Ai3Id, "Mina", 1.3f, 2.1f),
                Ai(RaceTestSupport.Ai4Id, "Rex", 1.4f, 2.2f)
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
            float maximumDelay = 1f)
        {
            return new RaceEventConfigAsset.RacerAuthoringData(
                id,
                name,
                RacerType.Ai,
                minimumDelay,
                maximumDelay);
        }
    }
}
