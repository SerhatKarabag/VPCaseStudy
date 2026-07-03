using DG.Tweening;
using UnityEngine;

namespace ThreadRace.Presentation.Animation
{
    [DisallowMultipleComponent]
    public sealed class FloatingUiElementAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _floatPixels = 12f;
        [SerializeField] private float _cycleDurationSeconds = 1.8f;
        [SerializeField] private float _phaseOffsetSeconds;

        private Tween _tween;
        private Vector2 _baseAnchoredPosition;
        private bool _hasBasePose;

        private void Awake()
        {
            EnsureTarget();
            CaptureBasePose();
        }

        private void OnEnable()
        {
            EnsureTarget();
            CaptureBasePose();
            StartAnimation();
        }

        private void OnDisable()
        {
            StopAnimation(true);
        }

        private void OnDestroy()
        {
            StopAnimation(false);
        }

        private void StartAnimation()
        {
            if (!Application.isPlaying)
            {
                return;
            }

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

            var raisedPosition = _baseAnchoredPosition + Vector2.up * _floatPixels;
            var halfCycleDuration = _cycleDurationSeconds * 0.5f;
            _tween = DOTween.To(
                    () => _target.anchoredPosition,
                    value => _target.anchoredPosition = value,
                    raisedPosition,
                    halfCycleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_phaseOffsetSeconds > 0f)
            {
                _tween.SetDelay(_phaseOffsetSeconds);
            }
        }

        private void StopAnimation(bool restorePose)
        {
            if (_tween != null && _tween.IsActive())
            {
                _tween.Kill();
            }

            _tween = null;

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

            _baseAnchoredPosition = _target.anchoredPosition;
            _hasBasePose = true;
        }

        private void RestoreBasePose()
        {
            if (!_hasBasePose || _target == null)
            {
                return;
            }

            _target.anchoredPosition = _baseAnchoredPosition;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _floatPixels = Mathf.Max(0f, _floatPixels);
            _cycleDurationSeconds = Mathf.Max(0.2f, _cycleDurationSeconds);
            _phaseOffsetSeconds = Mathf.Max(0f, _phaseOffsetSeconds);
        }
#endif
    }
}
