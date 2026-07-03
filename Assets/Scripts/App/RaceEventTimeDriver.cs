using System;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceEventTimeDriver : ITickable
    {
        private const long NoActiveCountdownPublished = -1L;

        private readonly RaceEventController _controller;
        private readonly RaceEventSettings _settings;
        private readonly IRaceSnapshotPublisher _snapshotPublisher;
        private readonly IRaceCountdownPublisher _countdownPublisher;

        private long _lastPublishedRemainingSeconds = NoActiveCountdownPublished;

        public RaceEventTimeDriver(
            RaceEventController controller,
            RaceEventSettings settings,
            IRaceSnapshotPublisher snapshotPublisher,
            IRaceCountdownPublisher countdownPublisher)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _snapshotPublisher = snapshotPublisher ?? throw new ArgumentNullException(nameof(snapshotPublisher));
            _countdownPublisher = countdownPublisher ?? throw new ArgumentNullException(nameof(countdownPublisher));
        }

        public void Tick()
        {
            var countdown = _controller.CurrentCountdown;
            if (!countdown.IsActive)
            {
                if (_lastPublishedRemainingSeconds != NoActiveCountdownPublished)
                {
                    _lastPublishedRemainingSeconds = NoActiveCountdownPublished;
                    _countdownPublisher.Publish(countdown);
                }

                return;
            }

            var secondsSinceLastPublish = _lastPublishedRemainingSeconds - countdown.RemainingSeconds;
            if (_lastPublishedRemainingSeconds < 0L
                || secondsSinceLastPublish >= _settings.CountdownUpdateIntervalSeconds
                || (countdown.IsExpired && _lastPublishedRemainingSeconds != 0L))
            {
                _lastPublishedRemainingSeconds = countdown.RemainingSeconds;
                _countdownPublisher.Publish(countdown);
            }

            if (_controller.Phase == RacePhase.Running && countdown.IsExpired && _controller.ResolveExpirationIfNeeded())
            {
                _snapshotPublisher.Publish(_controller.CurrentSnapshot);
                _countdownPublisher.Publish(_controller.CurrentCountdown);
            }
        }
    }
}
