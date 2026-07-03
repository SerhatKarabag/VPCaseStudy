using System;
using ThreadRace.Core.Progress;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using Zenject;

namespace ThreadRace.Presentation.Presenters
{
    public sealed class PlaceholderLevelPresenter : IInitializable, IDisposable
    {
        private readonly IPlaceholderLevelView _view;
        private readonly ILevelResultReporter _levelResultReporter;
        private readonly IRaceSnapshotProvider _snapshotProvider;
        private readonly IHostLevelProgressService _hostLevelProgressService;
        private readonly SignalBus _signalBus;

        private bool _initialized;
        private LevelResult? _pendingResult;
        private PlaceholderLevelScreen _screen = PlaceholderLevelScreen.Challenge;

        public PlaceholderLevelPresenter(
            IPlaceholderLevelView view,
            ILevelResultReporter levelResultReporter,
            IRaceSnapshotProvider snapshotProvider,
            IHostLevelProgressService hostLevelProgressService,
            SignalBus signalBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelResultReporter = levelResultReporter ?? throw new ArgumentNullException(nameof(levelResultReporter));
            _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
            _hostLevelProgressService = hostLevelProgressService ?? throw new ArgumentNullException(nameof(hostLevelProgressService));
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _view.SuccessRequested += OnSuccessRequested;
            _view.FailRequested += OnFailRequested;
            _view.LevelWinClaimRequested += OnLevelWinClaimRequested;
            _view.LevelFailReturnRequested += OnLevelFailReturnRequested;
            _signalBus.Subscribe<HostGameplayStartedSignal>(OnHostGameplayStarted);
            _signalBus.Subscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _initialized = true;
            var currentHostLevel = _hostLevelProgressService.LoadCurrentLevel();
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
            PublishHostLevel(currentHostLevel);
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _view.SuccessRequested -= OnSuccessRequested;
            _view.FailRequested -= OnFailRequested;
            _view.LevelWinClaimRequested -= OnLevelWinClaimRequested;
            _view.LevelFailReturnRequested -= OnLevelFailReturnRequested;
            _signalBus.Unsubscribe<HostGameplayStartedSignal>(OnHostGameplayStarted);
            _signalBus.Unsubscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _initialized = false;
        }

        public void ApplySnapshot(RaceSnapshot snapshot)
        {
            _view.Render(BuildModel(snapshot, _screen, _hostLevelProgressService.CurrentLevel));
        }

        public static PlaceholderLevelModel BuildModel(RaceSnapshot snapshot)
        {
            return BuildModel(snapshot, PlaceholderLevelScreen.Challenge, 1);
        }

        public static PlaceholderLevelModel BuildModel(RaceSnapshot snapshot, PlaceholderLevelScreen screen)
        {
            return BuildModel(snapshot, screen, 1);
        }

        public static PlaceholderLevelModel BuildModel(RaceSnapshot snapshot, PlaceholderLevelScreen screen, int currentHostLevel)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new PlaceholderLevelModel(
                FormatHostLevel(currentHostLevel),
                "Complete the placeholder challenge.",
                screen == PlaceholderLevelScreen.Challenge,
                screen,
                "+100 COINS");
        }

        private void OnSuccessRequested()
        {
            _pendingResult = LevelResult.Success;
            _screen = PlaceholderLevelScreen.LevelWin;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
            _signalBus.Fire(new HostGameplayScreenChangedSignal(_screen));
        }

        private void OnFailRequested()
        {
            _pendingResult = LevelResult.Fail;
            _screen = PlaceholderLevelScreen.LevelFail;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
            _signalBus.Fire(new HostGameplayScreenChangedSignal(_screen));
        }

        private void OnLevelWinClaimRequested()
        {
            CompleteHostGameplay();
        }

        private void OnLevelFailReturnRequested()
        {
            _signalBus.Fire(new HostGameplayBackHomeClickedSignal());
            CompleteHostGameplay();
        }

        private void CompleteHostGameplay()
        {
            if (!_pendingResult.HasValue)
            {
                return;
            }

            var result = _pendingResult.Value;
            _pendingResult = null;
            _screen = PlaceholderLevelScreen.Challenge;

            if (result == LevelResult.Success)
            {
                PublishHostLevel(_hostLevelProgressService.AdvanceAfterSuccess());
            }

            var snapshot = _snapshotProvider.CurrentSnapshot;
            var shouldOpenRacePopup = snapshot != null && snapshot.Phase == RacePhase.Running;
            if (snapshot != null)
            {
                ApplySnapshot(snapshot);
            }
            _signalBus.Fire(new HostGameplayCompletedSignal(result, shouldOpenRacePopup));

            _levelResultReporter.Report(result);
        }

        private void OnSnapshotChanged(RaceSnapshotChangedSignal signal)
        {
            ApplySnapshot(signal.Snapshot);
        }

        private void OnHostGameplayStarted(HostGameplayStartedSignal signal)
        {
            _pendingResult = null;
            _screen = PlaceholderLevelScreen.Challenge;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        private void PublishHostLevel(int currentLevel)
        {
            _signalBus.Fire(new HostLevelChangedSignal(currentLevel));
        }

        private static string FormatHostLevel(int currentHostLevel)
        {
            return "LEVEL " + Math.Max(1, currentHostLevel).ToString();
        }
    }
}
