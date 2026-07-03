using DG.Tweening;
using TMPro;
using ThreadRace.Presentation.Models;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    public sealed class RacerHudRowView : MonoBehaviour
    {
        [SerializeField] private string _racerId;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _finishText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Image _playerAccent;
        [SerializeField] private RectTransform _progressTrack;
        [SerializeField] private RectTransform _progressMover;
        [SerializeField] private RectTransform _rankFeedbackRoot;
        [SerializeField] private Image _leaderCrown;
        [SerializeField] private float _progressMarkerPadding = 34f;
        [SerializeField] private float _moveDurationSeconds = 0.22f;
        [SerializeField] private float _overtakePulseScale = 1.08f;
        [SerializeField] private float _overtakePulseDurationSeconds = 0.18f;

        private Tween _moveTween;
        private Tween _progressTween;
        private Tween _overtakeTween;
        private Tween _rankFeedbackTween;
        private bool _hasRendered;
        private int _lastRank;
        private int _lastProgress;
        private bool _wasFinished;
        private bool _hasSlotTarget;
        private Vector2 _lastSlotTargetPosition;
        private bool _hasProgressTarget;
        private Vector2 _lastProgressTargetPosition;

        public string RacerId => _racerId;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = transform as RectTransform;
                }

                return _rectTransform;
            }
        }

        public void Configure(string racerId)
        {
            _racerId = racerId;
        }

        public void Render(RacerHudRowModel model, RectTransform targetSlot)
        {
            if (model == null)
            {
                throw new System.ArgumentNullException(nameof(model));
            }

            if (_rankText != null)
            {
                _rankText.text = "#" + model.CurrentRank.ToString();
            }

            if (_nameText != null)
            {
                _nameText.text = model.DisplayName;
            }

            var hasFinishPlacement = model.IsFinished
                && model.FinishPlacement.HasValue
                && model.FinishPlacement.Value > 0;

            if (_progressText != null)
            {
                _progressText.text = hasFinishPlacement ? string.Empty : model.ProgressText;
                _progressText.gameObject.SetActive(!hasFinishPlacement);
            }

            if (_finishText != null)
            {
                _finishText.text = hasFinishPlacement
                    ? FormatFinishPlacement(model.FinishPlacement.Value)
                    : string.Empty;
                _finishText.gameObject.SetActive(hasFinishPlacement);
            }

            if (_progressFill != null)
            {
                _progressFill.fillAmount = Mathf.Clamp01(model.NormalizedProgress);
            }

            MoveProgressMarker(model.NormalizedProgress);

            if (_playerAccent != null)
            {
                _playerAccent.enabled = model.IsPlayer;
            }

            if (_leaderCrown != null)
            {
                _leaderCrown.gameObject.SetActive(model.CurrentRank == 1);
            }

            MoveToSlot(targetSlot);
            PlayStateChangeFeedback(model);

            _lastRank = model.CurrentRank;
            _lastProgress = model.Progress;
            _wasFinished = model.IsFinished;
            _hasRendered = true;
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _progressTween?.Kill();
            _progressTween = null;
            _overtakeTween?.Kill();
            _overtakeTween = null;
            _rankFeedbackTween?.Kill();
            _rankFeedbackTween = null;
        }

        private void MoveToSlot(RectTransform targetSlot)
        {
            if (targetSlot == null || RectTransform == null)
            {
                return;
            }

            var targetPosition = targetSlot.anchoredPosition;
            if (_hasSlotTarget && Approximately(_lastSlotTargetPosition, targetPosition))
            {
                if (_moveTween != null && _moveTween.IsActive())
                {
                    return;
                }

                if (Approximately(RectTransform.anchoredPosition, targetPosition))
                {
                    return;
                }
            }

            _lastSlotTargetPosition = targetPosition;
            _hasSlotTarget = true;
            _moveTween?.Kill();

            if (!Application.isPlaying || _moveDurationSeconds <= 0f)
            {
                RectTransform.anchoredPosition = targetPosition;
                return;
            }

            _moveTween = DOTween
                .To(() => RectTransform.anchoredPosition, value => RectTransform.anchoredPosition = value, targetPosition, _moveDurationSeconds)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void PlayStateChangeFeedback(RacerHudRowModel model)
        {
            if (!_hasRendered || !Application.isPlaying)
            {
                return;
            }

            var rankImproved = model.CurrentRank < _lastRank;
            var progressed = model.Progress > _lastProgress;
            var justFinished = model.IsFinished && !_wasFinished;

            if (rankImproved)
            {
                PlayOvertakeFeedback();
                return;
            }

            if (justFinished || progressed)
            {
                PlayRankFeedback(1.04f, 0.12f);
            }
        }

        private void PlayOvertakeFeedback()
        {
            var rowTransform = RectTransform;
            if (rowTransform != null)
            {
                _overtakeTween?.Kill();
                rowTransform.localScale = Vector3.one;
                _overtakeTween = DOTween
                    .To(() => rowTransform.localScale, value => rowTransform.localScale = value, Vector3.one * _overtakePulseScale, _overtakePulseDurationSeconds)
                    .SetEase(Ease.OutBack)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true);
            }

            PlayRankFeedback(1.18f, _overtakePulseDurationSeconds);
        }

        private void PlayRankFeedback(float scale, float durationSeconds)
        {
            var target = GetRankFeedbackRoot();
            if (target == null)
            {
                return;
            }

            _rankFeedbackTween?.Kill();
            target.localScale = Vector3.one;
            _rankFeedbackTween = DOTween
                .To(() => target.localScale, value => target.localScale = value, Vector3.one * scale, durationSeconds)
                .SetEase(Ease.OutBack)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private RectTransform GetRankFeedbackRoot()
        {
            if (_rankFeedbackRoot != null)
            {
                return _rankFeedbackRoot;
            }

            if (_rankText != null && _rankText.transform.parent != null)
            {
                _rankFeedbackRoot = _rankText.transform.parent as RectTransform;
            }

            return _rankFeedbackRoot;
        }

        private void MoveProgressMarker(float normalizedProgress)
        {
            if (_progressTrack == null || _progressMover == null)
            {
                return;
            }

            var trackWidth = _progressTrack.rect.width;
            if (trackWidth <= 1f)
            {
                trackWidth = _progressTrack.sizeDelta.x;
            }

            if (trackWidth <= 1f)
            {
                return;
            }

            var clampedProgress = Mathf.Clamp01(normalizedProgress);
            var left = -trackWidth * _progressTrack.pivot.x + _progressMarkerPadding;
            var right = trackWidth * (1f - _progressTrack.pivot.x) - _progressMarkerPadding;
            var targetPosition = _progressMover.anchoredPosition;
            targetPosition.x = Mathf.Lerp(left, right, clampedProgress);

            if (_hasProgressTarget && Approximately(_lastProgressTargetPosition, targetPosition))
            {
                if (_progressTween != null && _progressTween.IsActive())
                {
                    return;
                }

                if (Approximately(_progressMover.anchoredPosition, targetPosition))
                {
                    return;
                }
            }

            _lastProgressTargetPosition = targetPosition;
            _hasProgressTarget = true;
            _progressTween?.Kill();
            if (!Application.isPlaying || _moveDurationSeconds <= 0f)
            {
                _progressMover.anchoredPosition = targetPosition;
                return;
            }

            _progressTween = DOTween
                .To(() => _progressMover.anchoredPosition, value => _progressMover.anchoredPosition = value, targetPosition, _moveDurationSeconds)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
        }

        private static string FormatFinishPlacement(int placement)
        {
            var lastTwoDigits = placement % 100;
            if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            {
                return placement.ToString() + "th";
            }

            switch (placement % 10)
            {
                case 1:
                    return placement.ToString() + "st";
                case 2:
                    return placement.ToString() + "nd";
                case 3:
                    return placement.ToString() + "rd";
                default:
                    return placement.ToString() + "th";
            }
        }
    }
}
