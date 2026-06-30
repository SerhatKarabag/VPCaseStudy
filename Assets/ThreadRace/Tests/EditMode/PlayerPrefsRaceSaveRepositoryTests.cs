using NUnit.Framework;
using ThreadRace.Gameplay.Persistence;
using ThreadRace.Infrastructure.Persistence;
using UnityEngine;

namespace ThreadRace.Tests.EditMode
{
    public sealed class PlayerPrefsRaceSaveRepositoryTests
    {
        private const string TestKey = "ThreadRace.Tests.PlayerPrefsSave";
        private const string OtherKey = "ThreadRace.Tests.PlayerPrefsOther";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(TestKey);
            PlayerPrefs.DeleteKey(OtherKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TestKey);
            PlayerPrefs.DeleteKey(OtherKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void MissingKey_ReturnsNotFound()
        {
            var result = new PlayerPrefsRaceSaveRepository().Load(TestKey);

            Assert.AreEqual(RaceSaveLoadStatus.NotFound, result.Status);
        }

        [Test]
        public void SaveThenLoad_ReturnsEquivalentSaveData()
        {
            var repository = new PlayerPrefsRaceSaveRepository();
            var settings = RaceTestSupport.CreateSettings(rangedAiDelays: true);
            var saveData = RaceTestSupport.CaptureStartedSave(settings);

            repository.Save(TestKey, saveData);
            var result = repository.Load(TestKey);

            Assert.AreEqual(RaceSaveLoadStatus.Loaded, result.Status);
            Assert.AreEqual(RaceTestSupport.SaveSignature(saveData), RaceTestSupport.SaveSignature(result.SaveData));
        }

        [Test]
        public void Clear_RemovesOnlyConfiguredKey()
        {
            var repository = new PlayerPrefsRaceSaveRepository();
            var settings = RaceTestSupport.CreateSettings(rangedAiDelays: true);
            repository.Save(TestKey, RaceTestSupport.CaptureStartedSave(settings));
            PlayerPrefs.SetString(OtherKey, "keep");
            PlayerPrefs.Save();

            repository.Clear(TestKey);

            Assert.IsFalse(PlayerPrefs.HasKey(TestKey));
            Assert.IsTrue(PlayerPrefs.HasKey(OtherKey));
        }

        [Test]
        public void MalformedJson_ProducesControlledFailure()
        {
            PlayerPrefs.SetString(TestKey, "{ malformed json");
            PlayerPrefs.Save();

            var result = new PlayerPrefsRaceSaveRepository().Load(TestKey);

            Assert.AreEqual(RaceSaveLoadStatus.Failed, result.Status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Test]
        public void EmptyJson_ProducesControlledFailure()
        {
            PlayerPrefs.SetString(TestKey, " ");
            PlayerPrefs.Save();

            var result = new PlayerPrefsRaceSaveRepository().Load(TestKey);

            Assert.AreEqual(RaceSaveLoadStatus.Failed, result.Status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Test]
        public void NegativeRandomConsumedCount_ProducesControlledFailure()
        {
            PlayerPrefs.SetString(
                TestKey,
                "{\"schemaVersion\":1,\"phase\":0,\"racers\":[],\"finishOrder\":[],\"randomConsumedCount\":-1,\"revision\":0}");
            PlayerPrefs.Save();

            var result = new PlayerPrefsRaceSaveRepository().Load(TestKey);

            Assert.AreEqual(RaceSaveLoadStatus.Failed, result.Status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }
    }
}
