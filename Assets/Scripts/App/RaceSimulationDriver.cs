using System;
using ThreadRace.Core.Time;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Domain;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceSimulationDriver : ITickable
    {
        private readonly RaceEventController _controller;
        private readonly IRaceTimeProvider _timeProvider;
        private readonly IRaceSnapshotPublisher _snapshotPublisher;
        private readonly IRaceCountdownPublisher _countdownPublisher;

        public RaceSimulationDriver(RaceEventController controller, IRaceTimeProvider timeProvider)
            : this(controller, timeProvider, NullRaceSnapshotPublisher.Instance, NullRaceCountdownPublisher.Instance)
        {
        }

        [Inject]
        public RaceSimulationDriver(
            RaceEventController controller,
            IRaceTimeProvider timeProvider,
            IRaceSnapshotPublisher snapshotPublisher,
            IRaceCountdownPublisher countdownPublisher)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _snapshotPublisher = snapshotPublisher ?? throw new ArgumentNullException(nameof(snapshotPublisher));
            _countdownPublisher = countdownPublisher ?? throw new ArgumentNullException(nameof(countdownPublisher));
        }

        public void Tick()
        {
            if (_controller.Phase != RacePhase.Running)
            {
                return;
            }

            var deltaTime = _timeProvider.UnscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_controller.AdvanceAi(deltaTime))
            {
                _snapshotPublisher.Publish(_controller.CurrentSnapshot);
                _countdownPublisher.Publish(_controller.CurrentCountdown);
            }
        }

        private sealed class NullRaceSnapshotPublisher : IRaceSnapshotPublisher
        {
            public static readonly NullRaceSnapshotPublisher Instance = new NullRaceSnapshotPublisher();

            private NullRaceSnapshotPublisher()
            {
            }

            public void Publish(RaceSnapshot snapshot)
            {
            }
        }

        private sealed class NullRaceCountdownPublisher : IRaceCountdownPublisher
        {
            public static readonly NullRaceCountdownPublisher Instance = new NullRaceCountdownPublisher();

            private NullRaceCountdownPublisher()
            {
            }

            public void Publish(RaceCountdownSnapshot snapshot)
            {
            }
        }
    }
}
