using DG.Tweening;
using UnityEngine;

namespace ThreadRace.Presentation.Navigation
{
    [DisallowMultipleComponent]
    public sealed class LockedNavbarTooltip : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _backgroundRoot;
        [SerializeField] private bool _mirrorBackgroundWhenSourceIsLeftOfTooltip = true;

        [Header("Open")]
        [Range(0f, 1f)]
        [SerializeField] private float _startScaleMultiplier = 0.12f;
        [SerializeField] private float _showDuration = 0.29f;
        [Range(0.6f, 2f)]
        [SerializeField] private float _showBackOvershoot = 1.15f;
        [SerializeField] private Ease _showMoveEase = Ease.OutBack;
        [SerializeField] private float _fallbackShowYOffset = 10f;

        [Header("Hide")]
        [Range(0f, 1f)]
        [SerializeField] private float _hideScaleMultiplier = 0.82f;
        [SerializeField] private float _hideDuration = 0.16f;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        private Sequence _sequence;
        private Vector3 _visibleScale = Vector3.one;
        private Vector3 _backgroundVisibleScale = Vector3.one;
        private Vector2 _visibleAnchoredPosition;
        private Canvas _parentCanvas;
        private RectTransform _rootParentRect;
        private bool _initialized;

        public bool IsVisible => _root != null && _root.gameObject.activeInHierarchy;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        public void SetVisibleAnchoredPosition(Vector2 anchoredPosition)
        {
            EnsureInitialized();
            _visibleAnchoredPosition = anchoredPosition;
            if (_root != null && !_root.gameObject.activeSelf)
            {
                _root.anchoredPosition = anchoredPosition;
            }
        }

        public void ShowFrom(RectTransform sourceRect)
        {
            EnsureInitialized();
            if (_root == null)
            {
                return;
            }

            KillSequence();

            var rootObject = _root.gameObject;
            if (!rootObject.activeSelf)
            {
                rootObject.SetActive(true);
            }

            _root.SetAsLastSibling();

            var startPosition = _visibleAnchoredPosition + new Vector2(0f, -_fallbackShowYOffset);
            var sourcePosition = startPosition;
            var hasSourcePosition = TryResolveSourceAnchoredPosition(sourceRect, ref sourcePosition);
            if (hasSourcePosition)
            {
                startPosition = sourcePosition;
            }

            _root.anchoredPosition = startPosition;
            _root.localScale = _visibleScale * _startScaleMultiplier;
            ApplyBackgroundOrientation(hasSourcePosition, sourcePosition);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(rootObject, LinkBehaviour.KillOnDestroy);

            if (_canvasGroup != null)
            {
                _sequence.Insert(0f, DOTween.To(
                    () => _canvasGroup.alpha,
                    value => _canvasGroup.alpha = value,
                    1f,
                    _showDuration).SetEase(Ease.OutSine));
            }

            _sequence.Insert(0f, DOTween.To(
                () => _root.localScale,
                value => _root.localScale = value,
                _visibleScale,
                _showDuration).SetEase(_showMoveEase, _showBackOvershoot));
            _sequence.Insert(0f, DOTween.To(
                () => _root.anchoredPosition,
                value => _root.anchoredPosition = value,
                _visibleAnchoredPosition,
                _showDuration).SetEase(_showMoveEase));
            _sequence.OnComplete(() =>
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f;
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                }

                _root.localScale = _visibleScale;
                _root.anchoredPosition = _visibleAnchoredPosition;
                _sequence = null;
            });
        }

        public void Hide()
        {
            EnsureInitialized();
            if (_root == null)
            {
                return;
            }

            var rootObject = _root.gameObject;
            if (!rootObject.activeSelf)
            {
                return;
            }

            KillSequence();

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(rootObject, LinkBehaviour.KillOnDestroy);

            if (_canvasGroup != null)
            {
                _sequence.Insert(0f, DOTween.To(
                    () => _canvasGroup.alpha,
                    value => _canvasGroup.alpha = value,
                    0f,
                    _hideDuration).SetEase(Ease.InSine));
            }

            _sequence.Insert(0f, DOTween.To(
                () => _root.localScale,
                value => _root.localScale = value,
                _visibleScale * _hideScaleMultiplier,
                _hideDuration).SetEase(_hideEase));
            _sequence.OnComplete(() =>
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                }

                if (rootObject != null)
                {
                    rootObject.SetActive(false);
                }

                _root.localScale = _visibleScale * _hideScaleMultiplier;
                _root.anchoredPosition = _visibleAnchoredPosition;
                _sequence = null;
            });
        }

        public void HideImmediate()
        {
            EnsureInitialized();
            if (_root == null)
            {
                return;
            }

            KillSequence();
            _root.localScale = _visibleScale * _hideScaleMultiplier;
            _root.anchoredPosition = _visibleAnchoredPosition;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            EnsureInitialized();
            if (_root == null || !_root.gameObject.activeInHierarchy)
            {
                return false;
            }

            var eventCamera = ResolveEventCamera();
            return RectTransformUtility.RectangleContainsScreenPoint(_root, screenPoint, eventCamera);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (_root == null)
            {
                _root = transform as RectTransform;
            }

            if (_root == null)
            {
                Debug.LogWarning("[LockedNavbarTooltip] RectTransform is required.", this);
                return;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = _root.GetComponent<CanvasGroup>();
            }

            if (_backgroundRoot == null)
            {
                var background = _root.Find("Background");
                _backgroundRoot = background as RectTransform;
            }

            var canvases = _root.GetComponentsInParent<Canvas>(true);
            _parentCanvas = canvases.Length > 0 ? canvases[0] : null;
            _rootParentRect = _root.parent as RectTransform;
            _visibleScale = _root.localScale;
            _backgroundVisibleScale = _backgroundRoot != null ? _backgroundRoot.localScale : Vector3.one;
            _visibleAnchoredPosition = _root.anchoredPosition;
            _initialized = true;
        }

        private void ApplyBackgroundOrientation(bool hasSourcePosition, Vector2 sourcePosition)
        {
            if (!_mirrorBackgroundWhenSourceIsLeftOfTooltip ||
                !hasSourcePosition ||
                _backgroundRoot == null ||
                _backgroundRoot == _root)
            {
                return;
            }

            var scale = _backgroundVisibleScale;
            scale.x = Mathf.Abs(scale.x);
            if (sourcePosition.x < _visibleAnchoredPosition.x)
            {
                scale.x = -scale.x;
            }

            _backgroundRoot.localScale = scale;
        }

        private bool TryResolveSourceAnchoredPosition(RectTransform sourceRect, ref Vector2 resolvedPosition)
        {
            if (sourceRect == null || _rootParentRect == null)
            {
                return false;
            }

            var eventCamera = ResolveEventCamera();
            var worldCenter = sourceRect.TransformPoint(sourceRect.rect.center);
            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootParentRect, screenPoint, eventCamera, out var localPoint))
            {
                return false;
            }

            resolvedPosition = localPoint;
            return true;
        }

        private Camera ResolveEventCamera()
        {
            if (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return _parentCanvas.worldCamera;
            }

            return null;
        }

        private void KillSequence()
        {
            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Kill();
            }

            _sequence = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _startScaleMultiplier = Mathf.Clamp01(_startScaleMultiplier);
            _showDuration = Mathf.Max(0.05f, _showDuration);
            _showBackOvershoot = Mathf.Clamp(_showBackOvershoot, 0.6f, 2f);
            _fallbackShowYOffset = Mathf.Max(0f, _fallbackShowYOffset);
            _hideScaleMultiplier = Mathf.Clamp01(_hideScaleMultiplier);
            _hideDuration = Mathf.Max(0.05f, _hideDuration);
        }
#endif
    }
}
