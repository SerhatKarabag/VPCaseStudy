using System;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.App
{
    public sealed class RaceUiCommandRouter : IRaceEventCommandHandler, IRaceSnapshotProvider, IRaceCountdownProvider
    {
        private readonly RaceEventController _controller;
        private readonly IRaceSnapshotPublisher _snapshotPublisher;
        private readonly IRaceCountdownPublisher _countdownPublisher;

        public RaceUiCommandRouter(
            RaceEventController controller,
            IRaceSnapshotPublisher snapshotPublisher,
            IRaceCountdownPublisher countdownPublisher)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _snapshotPublisher = snapshotPublisher ?? throw new ArgumentNullException(nameof(snapshotPublisher));
            _countdownPublisher = countdownPublisher ?? throw new ArgumentNullException(nameof(countdownPublisher));
        }

        public RaceSnapshot CurrentSnapshot => _controller.CurrentSnapshot;

        public RaceCountdownSnapshot CurrentCountdown => _controller.CurrentCountdown;

        public bool StartRace()
        {
            var changed = _controller.StartNewRace();
            if (changed)
            {
                PublishCurrentSnapshot();
            }

            return changed;
        }

        public bool ReportLevelResult(LevelResult result)
        {
            var wasRunning = _controller.Phase == RacePhase.Running;
            var changed = _controller.ReportLevelResult(result);
            if (changed || (wasRunning && result == LevelResult.Fail))
            {
                PublishCurrentSnapshot();
            }

            return changed;
        }

        public bool ResolveExpiredEvent()
        {
            var changed = _controller.ResolveExpiredEventIfNeeded();
            if (changed)
            {
                PublishCurrentSnapshot();
            }

            return changed;
        }

        public bool ClaimReward()
        {
            var changed = _controller.ClaimReward();
            if (changed)
            {
                PublishCurrentSnapshot();
            }

            return changed;
        }

        public void ResetRace()
        {
            _controller.Reset();
            PublishCurrentSnapshot();
        }

        public void PublishCurrentSnapshot()
        {
            _snapshotPublisher.Publish(_controller.CurrentSnapshot);
            _countdownPublisher.Publish(_controller.CurrentCountdown);
        }
    }
}
