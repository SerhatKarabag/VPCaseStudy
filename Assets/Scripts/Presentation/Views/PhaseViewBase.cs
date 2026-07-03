using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PhaseViewBase : MonoBehaviour, IPhaseView
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _animatedRoot;
        [SerializeField] private float _fadeDurationSeconds = 0.16f;
        [SerializeField] private float _springInDurationSeconds = 0.36f;
        [SerializeField] private float _springInStartScale = 0.84f;
        [SerializeField] private Ease _springInEase = Ease.OutBack;
        [SerializeField] private float _dismissDurationSeconds = 0.12f;
        [SerializeField] private float _dismissTargetScale = 0.96f;

        private Tween _visibilityTween;
        private Tween _scaleTween;
        private CanvasGroup _layerCanvasGroup;

        public bool IsVisible { get; private set; }

        public bool IsInteractive => _canvasGroup != null && _canvasGroup.interactable && _canvasGroup.blocksRaycasts;

        public void WarmUpForFirstShow()
        {
            EnsureReferences();

            if (!Application.isPlaying)
            {
                return;
            }

            var target = AnimatedRoot;
            var originalScale = target == null ? Vector3.one : target.localScale;
            var root = transform as RectTransform;

            WarmUpTextMeshes();

            if (root != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            }

            Canvas.ForceUpdateCanvases();

            if (target != null)
            {
                target.localScale = originalScale;
            }
        }

        public virtual void SetVisible(bool visible)
        {
            EnsureReferences();

            if (IsVisible == visible && _visibilityTween != null && _visibilityTween.IsActive() && _visibilityTween.IsPlaying())
            {
                return;
            }

            if (IsVisible == visible && _canvasGroup.alpha == (visible ? 1f : 0f))
            {
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
                return;
            }

            IsVisible = visible;
            _visibilityTween?.Kill();
            _visibilityTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;

            if (!Application.isPlaying)
            {
                ApplyImmediateVisibility(visible);
                return;
            }

            if (visible)
            {
                PlaySpringIn();
            }
            else
            {
                PlayDismiss();
            }
        }

        private void ApplyLayerVisibility(bool visible)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var layerGroup = GetLayerCanvasGroup();
            if (layerGroup == null)
            {
                return;
            }

            layerGroup.alpha = visible ? 1f : 0f;
            layerGroup.interactable = visible;
            layerGroup.blocksRaycasts = visible;
        }

        private CanvasGroup GetLayerCanvasGroup()
        {
            if (_layerCanvasGroup == null && transform.parent != null)
            {
                _layerCanvasGroup = transform.parent.GetComponent<CanvasGroup>();
            }

            return _layerCanvasGroup;
        }

        protected virtual void Awake()
        {
            EnsureReferences();
            ApplyImmediateVisibility(false);
        }

        protected virtual void OnDestroy()
        {
            _visibilityTween?.Kill();
            _visibilityTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        protected RectTransform AnimatedRoot
        {
            get
            {
                EnsureReferences();
                return _animatedRoot;
            }
        }

        protected void PlayScalePulse(float scale = 1.05f, float durationSeconds = 0.12f)
        {
            var target = AnimatedRoot;
            if (target == null || !Application.isPlaying)
            {
                return;
            }

            _scaleTween?.Kill();
            target.localScale = Vector3.one;
            _scaleTween = DOTween
                .To(() => target.localScale, value => target.localScale = value, Vector3.one * scale, durationSeconds)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void EnsureReferences()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_animatedRoot == null)
            {
                _animatedRoot = transform as RectTransform;
            }
        }

        private void WarmUpTextMeshes()
        {
            var textComponents = GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < textComponents.Length; i++)
            {
                var text = textComponents[i];
                if (text != null)
                {
                    text.ForceMeshUpdate(true, false);
                }
            }
        }

        private void PlaySpringIn()
        {
            ApplyLayerVisibility(true);

            var target = AnimatedRoot;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (target != null)
            {
                target.localScale = Vector3.one * Mathf.Clamp(_springInStartScale, 0.1f, 1f);
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            var fadeDuration = Mathf.Max(0f, _fadeDurationSeconds);
            var springDuration = Mathf.Max(0f, _springInDurationSeconds);

            sequence.Insert(0f, DOTween
                .To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, 1f, fadeDuration)
                .SetEase(Ease.OutQuad));

            if (target != null)
            {
                sequence.Insert(0f, DOTween
                    .To(() => target.localScale, value => target.localScale = value, Vector3.one, springDuration)
                    .SetEase(_springInEase));
            }

            _visibilityTween = sequence.OnComplete(() =>
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _visibilityTween = null;

                if (target != null)
                {
                    target.localScale = Vector3.one;
                }

                OnShown();
            });
        }

        private void PlayDismiss()
        {
            var target = AnimatedRoot;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var sequence = DOTween.Sequence().SetUpdate(true);
            var duration = Mathf.Max(0f, _dismissDurationSeconds);

            sequence.Insert(0f, DOTween
                .To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, 0f, duration)
                .SetEase(Ease.InQuad));

            if (target != null && duration > 0f)
            {
                sequence.Insert(0f, DOTween
                    .To(() => target.localScale, value => target.localScale = value, Vector3.one * _dismissTargetScale, duration)
                    .SetEase(Ease.InQuad));
            }

            _visibilityTween = sequence.OnComplete(() =>
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                ApplyLayerVisibility(false);
                _visibilityTween = null;

                if (target != null)
                {
                    target.localScale = Vector3.one;
                }

                OnHidden();
            });
        }

        private void ApplyImmediateVisibility(bool visible)
        {
            _visibilityTween?.Kill();
            _visibilityTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;

            IsVisible = visible;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
            ApplyLayerVisibility(visible);

            var target = AnimatedRoot;
            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }
    }
}
