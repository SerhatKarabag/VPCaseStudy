using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Animation
{
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _pressedScale = 0.94f;
        [SerializeField] private float _pressDurationSeconds = 0.07f;
        [SerializeField] private float _releaseDurationSeconds = 0.16f;
        [SerializeField] private float _releaseOvershoot = 1.55f;

        private Tween _tween;
        private Button _button;

        public static void Install(Button button)
        {
            if (button == null)
            {
                return;
            }

            var feedback = button.GetComponent<ButtonPressFeedback>();
            if (feedback == null)
            {
                feedback = button.gameObject.AddComponent<ButtonPressFeedback>();
            }

            feedback.EnsureReferences();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            AnimateTo(Vector3.one * _pressedScale, _pressDurationSeconds, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            AnimateTo(Vector3.one, _releaseDurationSeconds, Ease.OutBack, _releaseOvershoot);
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;

            if (_target != null)
            {
                _target.localScale = Vector3.one;
            }
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _tween = null;
        }

        private void Release()
        {
            if (!CanAnimate())
            {
                return;
            }

            AnimateTo(Vector3.one, _releaseDurationSeconds, Ease.OutBack, _releaseOvershoot);
        }

        private bool CanAnimate()
        {
            EnsureReferences();
            return Application.isPlaying && _target != null && (_button == null || _button.interactable);
        }

        private void AnimateTo(Vector3 scale, float durationSeconds, Ease ease, float overshoot = 1.70158f)
        {
            _tween?.Kill();
            _tween = DOTween
                .To(() => _target.localScale, value => _target.localScale = value, scale, Mathf.Max(0.01f, durationSeconds))
                .SetEase(ease, overshoot)
                .SetUpdate(true);
        }

        private void EnsureReferences()
        {
            if (_target == null)
            {
                _target = transform as RectTransform;
            }

            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }
    }
}
