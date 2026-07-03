using System;
using ThreadRace.Core.Random;
using ThreadRace.Core.Time;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;

namespace ThreadRace.Gameplay.Application
{
    public sealed class RaceEventController : IRaceSnapshotProvider, IRaceCountdownProvider
    {
        private readonly RaceEventSettings _settings;
        private readonly IRaceSaveRepository _saveRepository;
        private readonly RaceSaveDataMapper _saveDataMapper;
        private readonly IDeterministicRandomSourceFactory _randomSourceFactory;
        private readonly IUtcClock _utcClock;

        private RaceSession _session;
        private RaceEventTimingState _timingState;
        private RaceCountdownSnapshot _cachedCountdownSnapshot;

        public RaceEventController(
            RaceEventSettings settings,
            IRaceSaveRepository saveRepository,
            RaceSaveDataMapper saveDataMapper,
            IDeterministicRandomSourceFactory randomSourceFactory,
            IUtcClock utcClock)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            _saveDataMapper = saveDataMapper ?? throw new ArgumentNullException(nameof(saveDataMapper));
            _randomSourceFactory = randomSourceFactory ?? throw new ArgumentNullException(nameof(randomSourceFactory));
            _utcClock = utcClock ?? throw new ArgumentNullException(nameof(utcClock));

            InitializeFromRepository();
        }

        public RaceControllerInitializationResult InitializationResult { get; private set; }

        public RacePhase Phase => _session.Phase;

        public int Revision => _session.Revision;

        public RaceSnapshot CurrentSnapshot => _session.GetSnapshot();

        public RaceCountdownSnapshot CurrentCountdown => CreateCountdownSnapshot();

        public RaceEventTimingState TimingState => _timingState;

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

            EnsureEventWindow();
            var effectiveNow = _timingState.GetEffectiveUtc(_utcClock.UtcNow);
            if (effectiveNow >= _timingState.EndUtc.Value)
            {
                _timingState = _timingState.WithLastObservedUtc(effectiveNow);
                return false;
            }

            if (seedOverride.HasValue)
            {
                _session = CreateFreshSession(seedOverride.Value);
            }

            _timingState = _timingState.WithLastObservedUtc(effectiveNow);
            _session.Start();
            SaveCurrentSession(updateObservedTime: false);
            return true;
        }

        public bool ReportLevelResult(LevelResult result)
        {
            ValidateLevelResult(result);

            if (_session.Phase != RacePhase.Running)
            {
                return false;
            }

            if (ResolveExpirationIfNeeded())
            {
                return true;
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

            if (ResolveExpirationIfNeeded())
            {
                return true;
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
            _timingState = CreateEventWindow(_utcClock.UtcNow);
            InitializationResult = RaceControllerInitializationResult.Reset();
        }

        public bool ApplyOfflineProgression()
        {
            if (_session.Phase != RacePhase.Running || !_timingState.HasStarted)
            {
                return false;
            }

            var revisionBefore = _session.Revision;
            var phaseBefore = _session.Phase;
            var effectiveNow = _timingState.GetEffectiveUtc(_utcClock.UtcNow);
            var catchUpBoundary = effectiveNow > _timingState.EndUtc.Value ? _timingState.EndUtc.Value : effectiveNow;
            var elapsedSeconds = _timingState.GetElapsedSecondsTo(catchUpBoundary);

            if (elapsedSeconds > 0L)
            {
                _session.AdvanceAi(elapsedSeconds);
            }

            var expired = false;
            if (_session.Phase == RacePhase.Running && effectiveNow >= _timingState.EndUtc.Value)
            {
                expired = _session.ExpireEvent();
            }

            _timingState = _timingState.WithLastObservedUtc(effectiveNow);

            var changed = revisionBefore != _session.Revision || phaseBefore != _session.Phase || elapsedSeconds > 0L || expired;
            if (changed)
            {
                SaveCurrentSession(updateObservedTime: false);
            }

            return changed;
        }

        public bool ResolveExpirationIfNeeded()
        {
            if (_session.Phase != RacePhase.Running || !_timingState.HasStarted)
            {
                return false;
            }

            return ResolveExpiredEventIfNeeded();
        }

        public bool ResolveExpiredEventIfNeeded()
        {
            if (_session.Phase == RacePhase.Reward || _session.Phase == RacePhase.Completed || !_timingState.HasStarted)
            {
                return false;
            }

            var effectiveNow = _timingState.GetEffectiveUtc(_utcClock.UtcNow);
            if (effectiveNow < _timingState.EndUtc.Value)
            {
                return false;
            }

            var changed = _session.ExpireEvent();
            _timingState = _timingState.WithLastObservedUtc(effectiveNow);
            if (changed)
            {
                SaveCurrentSession(updateObservedTime: false);
            }

            return changed;
        }

        public bool ClaimReward()
        {
            if (_session.Phase != RacePhase.Reward)
            {
                return false;
            }

            var changed = _session.ClaimReward();
            if (changed)
            {
                SaveCurrentSession();
            }

            return changed;
        }

        public bool CheckpointObservedTime()
        {
            if (!_timingState.HasStarted)
            {
                return false;
            }

            var before = _timingState.LastObservedUtc.Value;
            var effectiveNow = _timingState.GetEffectiveUtc(_utcClock.UtcNow);
            _timingState = _timingState.WithLastObservedUtc(effectiveNow);
            SaveCurrentSession(updateObservedTime: false);
            return _timingState.LastObservedUtc.Value > before;
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
                _timingState = CreateEventWindow(_utcClock.UtcNow);
                InitializationResult = RaceControllerInitializationResult.LoadFailed(exception.Message);
                return;
            }

            if (loadResult == null || loadResult.Status == RaceSaveLoadStatus.NotFound)
            {
                _session = CreateFreshSession(_settings.DefaultSeed);
                _timingState = CreateEventWindow(_utcClock.UtcNow);
                InitializationResult = RaceControllerInitializationResult.NoSave();
                return;
            }

            if (loadResult.Status == RaceSaveLoadStatus.Failed)
            {
                _session = CreateFreshSession(_settings.DefaultSeed);
                _timingState = CreateEventWindow(_utcClock.UtcNow);
                InitializationResult = RaceControllerInitializationResult.LoadFailed(loadResult.ErrorMessage);
                return;
            }

            try
            {
                _session = _saveDataMapper.Restore(_settings, loadResult.SaveData, _randomSourceFactory);
                _timingState = _saveDataMapper.RestoreTimingState(_settings, loadResult.SaveData);
                EnsureEventWindow();
                ApplyOfflineProgression();
                InitializationResult = RaceControllerInitializationResult.Restored();
            }
            catch (RaceSaveValidationException exception)
            {
                _session = CreateFreshSession(_settings.DefaultSeed);
                _timingState = CreateEventWindow(_utcClock.UtcNow);
                InitializationResult = RaceControllerInitializationResult.InvalidSave(exception.Message);
            }
        }

        private RaceSession CreateFreshSession(int seed)
        {
            return new RaceSession(_settings.RaceConfiguration, _randomSourceFactory.Create(seed));
        }

        private void SaveCurrentSession(bool updateObservedTime = true)
        {
            if (updateObservedTime && _timingState.HasStarted)
            {
                _timingState = _timingState.WithLastObservedUtc(_timingState.GetEffectiveUtc(_utcClock.UtcNow));
            }

            var saveData = _saveDataMapper.Capture(_settings, _session, _timingState);
            _saveRepository.Save(_settings.SaveKey, saveData);
        }

        private RaceCountdownSnapshot CreateCountdownSnapshot()
        {
            if (!_timingState.HasStarted)
            {
                return GetOrCreateCountdownSnapshot(false, 0L, false, null);
            }

            var effectiveNow = _timingState.GetEffectiveUtc(_utcClock.UtcNow);
            var remainingSeconds = _timingState.GetRemainingSeconds(effectiveNow);
            if (_session.Phase == RacePhase.Reward || _session.Phase == RacePhase.Completed)
            {
                return GetOrCreateCountdownSnapshot(
                    false,
                    0L,
                    remainingSeconds == 0L,
                    _timingState.EndUtc);
            }

            return GetOrCreateCountdownSnapshot(
                true,
                remainingSeconds,
                remainingSeconds == 0L,
                _timingState.EndUtc);
        }

        private RaceCountdownSnapshot GetOrCreateCountdownSnapshot(
            bool isActive,
            long remainingSeconds,
            bool isExpired,
            DateTimeOffset? eventEndUtc)
        {
            if (_cachedCountdownSnapshot != null
                && _cachedCountdownSnapshot.IsActive == isActive
                && _cachedCountdownSnapshot.RemainingSeconds == remainingSeconds
                && _cachedCountdownSnapshot.IsExpired == isExpired
                && _cachedCountdownSnapshot.EventEndUtc == eventEndUtc)
            {
                return _cachedCountdownSnapshot;
            }

            _cachedCountdownSnapshot = new RaceCountdownSnapshot(isActive, remainingSeconds, isExpired, eventEndUtc);
            return _cachedCountdownSnapshot;
        }

        private void EnsureEventWindow()
        {
            if (_timingState.HasStarted)
            {
                return;
            }

            _timingState = CreateEventWindow(_utcClock.UtcNow);
        }

        private RaceEventTimingState CreateEventWindow(DateTimeOffset currentUtc)
        {
            return RaceEventTimingState.Started(
                currentUtc,
                AddDuration(currentUtc, _settings.EventDurationSeconds),
                currentUtc);
        }

        private static DateTimeOffset AddDuration(DateTimeOffset startUtc, long durationSeconds)
        {
            try
            {
                return startUtc.AddSeconds(durationSeconds);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException("Configured event duration produces an invalid end timestamp.", exception);
            }
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
