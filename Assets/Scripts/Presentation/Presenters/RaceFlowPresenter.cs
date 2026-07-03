using System;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using Zenject;

namespace ThreadRace.Presentation.Presenters
{
    public sealed class RaceFlowPresenter : IInitializable, IDisposable
    {
        private readonly IMainMenuView _mainMenuView;
        private readonly IEntryPopupView _entryPopupView;
        private readonly IRaceHudView _raceHudView;
        private readonly IPlaceholderLevelView _placeholderLevelView;
        private readonly IRaceResultView _raceResultView;
        private readonly IRaceSnapshotProvider _snapshotProvider;
        private readonly IRaceCountdownProvider _countdownProvider;
        private readonly IRaceEventCommandHandler _commandHandler;
        private readonly SignalBus _signalBus;

        private bool _initialized;
        private bool _entryRequestedFromMenu;
        private bool _hostGameplayRequested;
        private bool _racePopupRequested;
        private bool _hasLastPhase;
        private RacePhase _lastPhase;

        public RaceFlowPresenter(
            IMainMenuView mainMenuView,
            IEntryPopupView entryPopupView,
            IRaceHudView raceHudView,
            IPlaceholderLevelView placeholderLevelView,
            IRaceResultView raceResultView,
            IRaceSnapshotProvider snapshotProvider,
            IRaceCountdownProvider countdownProvider,
            IRaceEventCommandHandler commandHandler,
            SignalBus signalBus)
        {
            _mainMenuView = mainMenuView ?? throw new ArgumentNullException(nameof(mainMenuView));
            _entryPopupView = entryPopupView ?? throw new ArgumentNullException(nameof(entryPopupView));
            _raceHudView = raceHudView ?? throw new ArgumentNullException(nameof(raceHudView));
            _placeholderLevelView = placeholderLevelView ?? throw new ArgumentNullException(nameof(placeholderLevelView));
            _raceResultView = raceResultView ?? throw new ArgumentNullException(nameof(raceResultView));
            _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
            _countdownProvider = countdownProvider ?? throw new ArgumentNullException(nameof(countdownProvider));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _signalBus.Subscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _signalBus.Subscribe<RaceCountdownChangedSignal>(OnCountdownChanged);
            _signalBus.Subscribe<HostLevelChangedSignal>(OnHostLevelChanged);
            _signalBus.Subscribe<HostGameplayCompletedSignal>(OnHostGameplayCompleted);
            _mainMenuView.PlayRequested += OnPlayRequested;
            _mainMenuView.ThreadRaceRequested += OnThreadRaceRequested;
            _entryPopupView.CloseRequested += OnEntryCloseRequested;
            _raceHudView.CloseRequested += OnRaceHudCloseRequested;
            _raceResultView.ContinueRequested += OnResultContinueRequested;
            _initialized = true;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _signalBus.Unsubscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _signalBus.Unsubscribe<RaceCountdownChangedSignal>(OnCountdownChanged);
            _signalBus.Unsubscribe<HostLevelChangedSignal>(OnHostLevelChanged);
            _signalBus.Unsubscribe<HostGameplayCompletedSignal>(OnHostGameplayCompleted);
            _mainMenuView.PlayRequested -= OnPlayRequested;
            _mainMenuView.ThreadRaceRequested -= OnThreadRaceRequested;
            _entryPopupView.CloseRequested -= OnEntryCloseRequested;
            _raceHudView.CloseRequested -= OnRaceHudCloseRequested;
            _raceResultView.ContinueRequested -= OnResultContinueRequested;
            _initialized = false;
        }

        public void ApplySnapshot(RaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var isNotStarted = snapshot.Phase == RacePhase.NotStarted;
            var isRunning = snapshot.Phase == RacePhase.Running;
            var isReward = snapshot.Phase == RacePhase.Reward;
            var isCompleted = snapshot.Phase == RacePhase.Completed;

            if (isNotStarted && _hasLastPhase && _lastPhase != RacePhase.NotStarted)
            {
                _entryRequestedFromMenu = false;
                _racePopupRequested = false;
                _hostGameplayRequested = false;
            }

            if (isRunning && _hasLastPhase && _lastPhase == RacePhase.NotStarted && _entryRequestedFromMenu)
            {
                _entryRequestedFromMenu = false;
                _racePopupRequested = true;
            }

            if (isCompleted && _hasLastPhase && _lastPhase == RacePhase.Reward)
            {
                _entryRequestedFromMenu = false;
                _racePopupRequested = false;
                _hostGameplayRequested = false;
            }

            var showHostGameplay = _hostGameplayRequested && _mainMenuView.IsAvailable;
            var showResultPopup = (isReward || isCompleted) && (!_mainMenuView.IsAvailable || _racePopupRequested);
            var showMainMenu = !showHostGameplay && _mainMenuView.IsAvailable && (isNotStarted || isRunning || isReward || isCompleted);
            var showEntry = !showHostGameplay && isNotStarted && (!_mainMenuView.IsAvailable || _entryRequestedFromMenu);
            var showRaceHud = !showHostGameplay && (showResultPopup || (isRunning && (!_mainMenuView.IsAvailable || _racePopupRequested)));
            var showResult = !showHostGameplay && showResultPopup;
            var mainMenuInteractive = showMainMenu && !showEntry && !showRaceHud && !showResult;

            _mainMenuView.SetVisible(showMainMenu, mainMenuInteractive);
            _entryPopupView.SetVisible(showEntry);
            _raceHudView.SetVisible(showRaceHud);
            _placeholderLevelView.SetVisible(showHostGameplay);
            _raceResultView.SetVisible(showResult);

            _lastPhase = snapshot.Phase;
            _hasLastPhase = true;
            ApplyCountdown(_countdownProvider.CurrentCountdown, snapshot.Phase);
        }

        private void OnSnapshotChanged(RaceSnapshotChangedSignal signal)
        {
            ApplySnapshot(signal.Snapshot);
        }

        private void OnCountdownChanged(RaceCountdownChangedSignal signal)
        {
            ApplyCountdown(signal.Snapshot, _snapshotProvider.CurrentSnapshot.Phase);
        }

        private void OnHostLevelChanged(HostLevelChangedSignal signal)
        {
            _mainMenuView.SetPlayButtonLabel(FormatHostLevel(signal.CurrentLevel));
        }

        private void OnPlayRequested()
        {
            _hostGameplayRequested = true;
            _entryRequestedFromMenu = false;
            _racePopupRequested = false;
            _signalBus.Fire(new HostGameplayStartedSignal());
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        private void OnThreadRaceRequested()
        {
            var snapshot = _snapshotProvider.CurrentSnapshot;
            if ((snapshot.Phase == RacePhase.NotStarted || snapshot.Phase == RacePhase.Running)
                && _countdownProvider.CurrentCountdown.IsExpired)
            {
                _hostGameplayRequested = false;
                _entryRequestedFromMenu = false;
                _racePopupRequested = true;
                _commandHandler.ResolveExpiredEvent();
                ApplySnapshot(_snapshotProvider.CurrentSnapshot);
                return;
            }

            if (snapshot.Phase == RacePhase.NotStarted)
            {
                _entryRequestedFromMenu = true;
                _racePopupRequested = false;
            }
            else if (snapshot.Phase == RacePhase.Running)
            {
                _entryRequestedFromMenu = false;
                _racePopupRequested = true;
            }
            else if (snapshot.Phase == RacePhase.Reward || snapshot.Phase == RacePhase.Completed)
            {
                _entryRequestedFromMenu = false;
                _racePopupRequested = true;
            }

            _hostGameplayRequested = false;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        private void OnEntryCloseRequested()
        {
            _entryRequestedFromMenu = false;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        private void OnRaceHudCloseRequested()
        {
            _racePopupRequested = false;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        private void OnResultContinueRequested()
        {
            if (_snapshotProvider.CurrentSnapshot.Phase != RacePhase.Completed)
            {
                return;
            }

            _entryRequestedFromMenu = false;
            _racePopupRequested = false;
            _hostGameplayRequested = false;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        private void OnHostGameplayCompleted(HostGameplayCompletedSignal signal)
        {
            _hostGameplayRequested = false;
            _entryRequestedFromMenu = false;
            _racePopupRequested = signal.ShouldOpenRacePopup;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        private void ApplyCountdown(RaceCountdownSnapshot countdown, RacePhase phase)
        {
            _mainMenuView.SetThreadRaceCountdown(RaceCountdownFormatter.FormatCompactCountdown(countdown, phase));
        }

        private static string FormatHostLevel(int currentLevel)
        {
            return "LEVEL " + Math.Max(1, currentLevel).ToString();
        }
    }
}
