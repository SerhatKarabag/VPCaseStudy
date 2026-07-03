using System;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using Zenject;

namespace ThreadRace.Presentation.Presenters
{
    public sealed class RaceHudPresenter : IInitializable, IDisposable
    {
        private readonly IRaceHudView _view;
        private readonly IRaceSnapshotProvider _snapshotProvider;
        private readonly IRaceCountdownProvider _countdownProvider;
        private readonly SignalBus _signalBus;

        private bool _initialized;
        private RaceSnapshot _lastSnapshot;
        private string _lastCountdownText = string.Empty;

        public RaceHudPresenter(
            IRaceHudView view,
            IRaceSnapshotProvider snapshotProvider,
            IRaceCountdownProvider countdownProvider,
            SignalBus signalBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
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

            _signalBus.Unsubscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _signalBus.Unsubscribe<RaceCountdownChangedSignal>(OnCountdownChanged);
            _initialized = false;
        }

        public void ApplySnapshot(RaceSnapshot snapshot)
        {
            _lastSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _lastCountdownText = FormatCountdownText(snapshot, _countdownProvider.CurrentCountdown);
            _view.Render(BuildModel(snapshot, _countdownProvider.CurrentCountdown));
        }

        public static RaceHudModel BuildModel(RaceSnapshot snapshot)
        {
            return BuildModel(snapshot, null);
        }

        public static RaceHudModel BuildModel(RaceSnapshot snapshot, RaceCountdownSnapshot countdown)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var rows = new RacerHudRowModel[snapshot.Racers.Count];
            for (var i = 0; i < snapshot.Racers.Count; i++)
            {
                var racer = snapshot.Racers[i];
                var targetSlotIndex = FindTargetSlot(snapshot, racer.Id);
                var normalizedProgress = snapshot.FinishTarget <= 0
                    ? 0f
                    : Clamp01((float)racer.Progress / snapshot.FinishTarget);

                rows[i] = new RacerHudRowModel(
                    racer.Id.Value,
                    racer.DisplayName,
                    racer.RacerType == RacerType.Player,
                    racer.CurrentRank,
                    racer.Progress,
                    snapshot.FinishTarget,
                    racer.Progress.ToString() + " / " + snapshot.FinishTarget.ToString(),
                    normalizedProgress,
                    racer.IsFinished,
                    racer.FinishPlacement,
                    targetSlotIndex);
            }

            return new RaceHudModel(rows, FormatCountdownText(snapshot, countdown));
        }

        private void OnSnapshotChanged(RaceSnapshotChangedSignal signal)
        {
            ApplySnapshot(signal.Snapshot);
        }

        private void OnCountdownChanged(RaceCountdownChangedSignal signal)
        {
            var snapshot = _lastSnapshot ?? _snapshotProvider.CurrentSnapshot;
            var countdownText = FormatCountdownText(snapshot, signal.Snapshot);
            if (string.Equals(countdownText, _lastCountdownText, StringComparison.Ordinal))
            {
                return;
            }

            _lastCountdownText = countdownText;
            _view.SetCountdownText(countdownText);
        }

        private static string FormatCountdownText(RaceSnapshot snapshot, RaceCountdownSnapshot countdown)
        {
            if (snapshot != null && (snapshot.Phase == RacePhase.Reward || snapshot.Phase == RacePhase.Completed))
            {
                return "ENDED";
            }

            if (countdown == null || !countdown.IsActive)
            {
                return string.Empty;
            }

            return RaceCountdownFormatter.FormatCompactCountdown(countdown);
        }

        private static int FindTargetSlot(RaceSnapshot snapshot, RacerId racerId)
        {
            for (var i = 0; i < snapshot.Ranking.Count; i++)
            {
                if (snapshot.Ranking[i].RacerId == racerId)
                {
                    return i;
                }
            }

            return 0;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
