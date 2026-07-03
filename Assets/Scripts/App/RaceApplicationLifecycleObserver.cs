using System;
using ThreadRace.Gameplay.Application;
using UnityEngine;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceApplicationLifecycleObserver : MonoBehaviour
    {
        private RaceEventController _controller;
        private IRaceSnapshotPublisher _snapshotPublisher;
        private IRaceCountdownPublisher _countdownPublisher;
        private bool _isBackgrounded;
        private bool _isQuitting;

        [Inject]
        public void Construct(
            RaceEventController controller,
            IRaceSnapshotPublisher snapshotPublisher,
            IRaceCountdownPublisher countdownPublisher)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _snapshotPublisher = snapshotPublisher ?? throw new ArgumentNullException(nameof(snapshotPublisher));
            _countdownPublisher = countdownPublisher ?? throw new ArgumentNullException(nameof(countdownPublisher));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CheckpointBeforeBackground();
                return;
            }

            ResumeFromBackground();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CheckpointBeforeBackground();
                return;
            }

            ResumeFromBackground();
        }

        private void OnApplicationQuit()
        {
            if (_isQuitting)
            {
                return;
            }

            _isQuitting = true;
            if (_controller != null)
            {
                _controller.CheckpointObservedTime();
            }
        }

        private void CheckpointBeforeBackground()
        {
            if (_isQuitting || _isBackgrounded || _controller == null)
            {
                return;
            }

            _isBackgrounded = true;
            _controller.CheckpointObservedTime();
            PublishCountdown();
        }

        private void ResumeFromBackground()
        {
            if (_isQuitting || !_isBackgrounded || _controller == null)
            {
                return;
            }

            _isBackgrounded = false;
            if (_controller.ApplyOfflineProgression())
            {
                _snapshotPublisher.Publish(_controller.CurrentSnapshot);
            }

            PublishCountdown();
        }

        private void PublishCountdown()
        {
            if (_countdownPublisher != null && _controller != null)
            {
                _countdownPublisher.Publish(_controller.CurrentCountdown);
            }
        }
    }
}
