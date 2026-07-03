using System;
using ThreadRace.Gameplay.Contracts;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RacePresentationBootstrap : IInitializable
    {
        private readonly IRaceSnapshotProvider _snapshotProvider;
        private readonly IRaceCountdownProvider _countdownProvider;
        private readonly IRaceSnapshotPublisher _snapshotPublisher;
        private readonly IRaceCountdownPublisher _countdownPublisher;

        public RacePresentationBootstrap(
            IRaceSnapshotProvider snapshotProvider,
            IRaceCountdownProvider countdownProvider,
            IRaceSnapshotPublisher snapshotPublisher,
            IRaceCountdownPublisher countdownPublisher)
        {
            _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
            _countdownProvider = countdownProvider ?? throw new ArgumentNullException(nameof(countdownProvider));
            _snapshotPublisher = snapshotPublisher ?? throw new ArgumentNullException(nameof(snapshotPublisher));
            _countdownPublisher = countdownPublisher ?? throw new ArgumentNullException(nameof(countdownPublisher));
        }

        public void Initialize()
        {
            _snapshotPublisher.Publish(_snapshotProvider.CurrentSnapshot);
            _countdownPublisher.Publish(_countdownProvider.CurrentCountdown);
        }
    }
}
