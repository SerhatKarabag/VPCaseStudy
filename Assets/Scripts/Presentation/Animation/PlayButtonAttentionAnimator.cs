using DG.Tweening;
using UnityEngine;

namespace ThreadRace.Presentation.Animation
{
    [DisallowMultipleComponent]
    public sealed class PlayButtonAttentionAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _target;

        [Header("Timing")]
        [SerializeField] private float _initialDelaySeconds = 1.15f;
        [SerializeField] private float _repeatDelaySeconds = 4.2f;

        [Header("Motion")]
        [SerializeField] private float _liftPixels = 10f;
        [SerializeField] private float _primaryScale = 1.08f;
        [SerializeField] private float _settleScale = 0.985f;
        [SerializeField] private float _secondaryScale = 1.035f;
        [SerializeField] private float _riseDurationSeconds = 0.2f;
        [SerializeField] private float _settleDurationSeconds = 0.1f;
        [SerializeField] private float _secondPulseDurationSeconds = 0.13f;
        [SerializeField] private float _returnDurationSeconds = 0.34f;
        [SerializeField] private float _backOvershoot = 1.22f;

        private Sequence _sequence;
        private Vector3 _baseScale;
        private Vector2 _baseAnchoredPosition;
        private bool _hasBasePose;
        private bool _shouldPlay = true;

        private void Awake()
        {
            EnsureTarget();
            CaptureBasePose();
        }

        private void OnEnable()
        {
            EnsureTarget();
            CaptureBasePose();

            if (_shouldPlay)
            {
                StartAnimation();
            }
        }

        private void OnDisable()
        {
            StopAnimation(true);
        }

        private void OnDestroy()
        {
            StopAnimation(false);
        }

        public void SetPlaying(bool playing)
        {
            if (_shouldPlay == playing)
            {
                if (playing && isActiveAndEnabled && (_sequence == null || !_sequence.IsActive()))
                {
                    StartAnimation();
                }

                return;
            }

            _shouldPlay = playing;

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (playing)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation(true);
            }
        }

        private void StartAnimation()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureTarget();
            if (_target == null)
            {
                return;
            }

            if (!_hasBasePose)
            {
                CaptureBasePose();
            }

            StopAnimation(false);
            RestoreBasePose();

            var raisedPosition = _baseAnchoredPosition + Vector2.up * _liftPixels;
            var primaryScale = _baseScale * _primaryScale;
            var settleScale = _baseScale * _settleScale;
            var secondaryScale = _baseScale * _secondaryScale;

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .AppendInterval(_initialDelaySeconds)
                .Append(DOTween.To(
                    () => _target.localScale,
                    value => _target.localScale = value,
                    primaryScale,
                    _riseDurationSeconds).SetEase(Ease.OutBack, _backOvershoot))
                .Join(DOTween.To(
                    () => _target.anchoredPosition,
                    value => _target.anchoredPosition = value,
                    raisedPosition,
                    _riseDurationSeconds).SetEase(Ease.OutCubic))
                .Append(DOTween.To(
                    () => _target.localScale,
                    value => _target.localScale = value,
                    settleScale,
                    _settleDurationSeconds).SetEase(Ease.InOutSine))
                .Join(DOTween.To(
                    () => _target.anchoredPosition,
                    value => _target.anchoredPosition = value,
                    _baseAnchoredPosition,
                    _settleDurationSeconds).SetEase(Ease.OutCubic))
                .Append(DOTween.To(
                    () => _target.localScale,
                    value => _target.localScale = value,
                    secondaryScale,
                    _secondPulseDurationSeconds).SetEase(Ease.OutBack, _backOvershoot))
                .Append(DOTween.To(
                    () => _target.localScale,
                    value => _target.localScale = value,
                    _baseScale,
                    _returnDurationSeconds).SetEase(Ease.OutElastic))
                .AppendInterval(_repeatDelaySeconds)
                .SetLoops(-1, LoopType.Restart);
        }

        private void StopAnimation(bool restorePose)
        {
            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Kill();
            }

            _sequence = null;

            if (restorePose)
            {
                RestoreBasePose();
            }
        }

        private void EnsureTarget()
        {
            if (_target == null)
            {
                _target = transform as RectTransform;
            }
        }

        private void CaptureBasePose()
        {
            if (_target == null)
            {
                return;
            }

            _baseScale = _target.localScale;
            _baseAnchoredPosition = _target.anchoredPosition;
            _hasBasePose = true;
        }

        private void RestoreBasePose()
        {
            if (!_hasBasePose || _target == null)
            {
                return;
            }

            _target.localScale = _baseScale;
            _target.anchoredPosition = _baseAnchoredPosition;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _initialDelaySeconds = Mathf.Max(0f, _initialDelaySeconds);
            _repeatDelaySeconds = Mathf.Max(0.5f, _repeatDelaySeconds);
            _liftPixels = Mathf.Max(0f, _liftPixels);
            _primaryScale = Mathf.Max(1f, _primaryScale);
            _settleScale = Mathf.Clamp(_settleScale, 0.8f, 1f);
            _secondaryScale = Mathf.Max(1f, _secondaryScale);
            _riseDurationSeconds = Mathf.Max(0.05f, _riseDurationSeconds);
            _settleDurationSeconds = Mathf.Max(0.05f, _settleDurationSeconds);
            _secondPulseDurationSeconds = Mathf.Max(0.05f, _secondPulseDurationSeconds);
            _returnDurationSeconds = Mathf.Max(0.05f, _returnDurationSeconds);
            _backOvershoot = Mathf.Clamp(_backOvershoot, 0.8f, 2f);
        }
#endif
    }
}
