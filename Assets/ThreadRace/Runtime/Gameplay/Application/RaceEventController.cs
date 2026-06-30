using System;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;

namespace ThreadRace.Gameplay.Application
{
    public sealed class RaceEventController : ILevelResultHandler
    {
        private readonly RaceEventSettings _settings;
        private readonly IRaceSaveRepository _saveRepository;
        private readonly RaceSaveDataMapper _saveDataMapper;
        private readonly IDeterministicRandomSourceFactory _randomSourceFactory;

        private RaceSession _session;

        public RaceEventController(
            RaceEventSettings settings,
            IRaceSaveRepository saveRepository,
            RaceSaveDataMapper saveDataMapper,
            IDeterministicRandomSourceFactory randomSourceFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            _saveDataMapper = saveDataMapper ?? throw new ArgumentNullException(nameof(saveDataMapper));
            _randomSourceFactory = randomSourceFactory ?? throw new ArgumentNullException(nameof(randomSourceFactory));

            InitializeFromRepository();
        }

        public RaceControllerInitializationResult InitializationResult { get; private set; }

        public RacePhase Phase => _session.Phase;

        public RaceSnapshot CurrentSnapshot => _session.GetSnapshot();

        public bool StartNewRace()
        {
            return StartNewRace(null);
        }

        public bool StartNewRace(int? seedOverride)
        {
            if (!InitializationResult.CanStartNewRace || _session.Phase != RacePhase.NotStarted)
            {
                return false;
            }

            var seed = seedOverride.GetValueOrDefault(_settings.DefaultSeed);
            _session = CreateFreshSession(seed);
            _session.Start();
            SaveCurrentSession();
            return true;
        }

        public bool ReportLevelResult(LevelResult result)
        {
            ValidateLevelResult(result);

            if (_session.Phase != RacePhase.Running)
            {
                return false;
            }

            var changed = _session.ApplyPlayerResult(result);
            if (changed)
            {
                SaveCurrentSession();
            }

            return changed;
        }

        public bool AdvanceAi(float deltaTimeSeconds)
        {
            if (_session.Phase != RacePhase.Running)
            {
                return false;
            }

            var changed = _session.AdvanceAi(deltaTimeSeconds);
            if (changed)
            {
                SaveCurrentSession();
            }

            return changed;
        }

        public void Reset()
        {
            _saveRepository.Clear(_settings.SaveKey);
            _session = CreateFreshSession(_settings.DefaultSeed);
            InitializationResult = RaceControllerInitializationResult.Reset();
        }

        private void InitializeFromRepository()
        {
            RaceSaveLoadResult loadResult;
            try
            {
                loadResult = _saveRepository.Load(_settings.SaveKey);
            }
            catch (Exception exception)
            {
                _session = CreateFreshSession(_settings.DefaultSeed);
                InitializationResult = RaceControllerInitializationResult.LoadFailed(exception.Message);
                return;
            }

            if (loadResult == null || loadResult.Status == RaceSaveLoadStatus.NotFound)
            {
                _session = CreateFreshSession(_settings.DefaultSeed);
                InitializationResult = RaceControllerInitializationResult.NoSave();
                return;
            }

            if (loadResult.Status == RaceSaveLoadStatus.Failed)
            {
                _session = CreateFreshSession(_settings.DefaultSeed);
                InitializationResult = RaceControllerInitializationResult.LoadFailed(loadResult.ErrorMessage);
                return;
            }

            try
            {
                _session = _saveDataMapper.Restore(_settings, loadResult.SaveData, _randomSourceFactory);
                InitializationResult = RaceControllerInitializationResult.Restored();
            }
            catch (RaceSaveValidationException exception)
            {
                _session = CreateFreshSession(_settings.DefaultSeed);
                InitializationResult = RaceControllerInitializationResult.InvalidSave(exception.Message);
            }
        }

        private RaceSession CreateFreshSession(int seed)
        {
            return new RaceSession(_settings.RaceConfiguration, _randomSourceFactory.Create(seed));
        }

        private void SaveCurrentSession()
        {
            var saveData = _saveDataMapper.Capture(_settings, _session);
            _saveRepository.Save(_settings.SaveKey, saveData);
        }

        private static void ValidateLevelResult(LevelResult result)
        {
            if (result != LevelResult.Success && result != LevelResult.Fail)
            {
                throw new ArgumentOutOfRangeException(nameof(result), "Unsupported level result.");
            }
        }
    }
}
