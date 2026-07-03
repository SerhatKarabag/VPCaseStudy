using System;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using Zenject;

namespace ThreadRace.Presentation.Presenters
{
    public sealed class EntryPopupPresenter : IInitializable, IDisposable
    {
        private readonly IEntryPopupView _view;
        private readonly IRaceEventCommandHandler _commandHandler;
        private readonly IRaceSnapshotProvider _snapshotProvider;
        private readonly IRaceCountdownProvider _countdownProvider;
        private readonly SignalBus _signalBus;

        private bool _initialized;
        private RaceSnapshot _lastSnapshot;

        public EntryPopupPresenter(
            IEntryPopupView view,
            IRaceEventCommandHandler commandHandler,
            IRaceSnapshotProvider snapshotProvider,
            IRaceCountdownProvider countdownProvider,
            SignalBus signalBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
            _countdownProvider = countdownProvider ?? throw new ArgumentNullException(nameof(countdownProvider));
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _view.StartRequested += OnStartRequested;
            _signalBus.Subscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _signalBus.Subscribe<RaceCountdownChangedSignal>(OnCountdownChanged);
            _initialized = true;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _view.StartRequested -= OnStartRequested;
            _signalBus.Unsubscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _signalBus.Unsubscribe<RaceCountdownChangedSignal>(OnCountdownChanged);
            _initialized = false;
        }

        public void ApplySnapshot(RaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _lastSnapshot = snapshot;
            Render(snapshot, _countdownProvider.CurrentCountdown);
        }

        private void OnStartRequested()
        {
            _commandHandler.StartRace();
        }

        private void OnSnapshotChanged(RaceSnapshotChangedSignal signal)
        {
            ApplySnapshot(signal.Snapshot);
        }

        private void OnCountdownChanged(RaceCountdownChangedSignal signal)
        {
            if (_lastSnapshot != null)
            {
                Render(_lastSnapshot, signal.Snapshot);
            }
        }

        private void Render(RaceSnapshot snapshot, RaceCountdownSnapshot countdown)
        {
            _view.SetContent(
                "THREAD RACE",
                "Reach " + snapshot.FinishTarget.ToString() + " successful levels before the other racers.",
                "Only the first " + snapshot.RewardedPositionCount.ToString() + " finishers earn a reward.",
                RaceCountdownFormatter.FormatTimeLeftLine(countdown));
            _view.SetStartEnabled(snapshot.Phase == RacePhase.NotStarted && countdown.IsActive && !countdown.IsExpired);
        }
    }
}
