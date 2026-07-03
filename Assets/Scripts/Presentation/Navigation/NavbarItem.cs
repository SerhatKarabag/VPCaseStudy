using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Navigation
{
    public sealed class NavbarItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("Visual Elements")]
        [SerializeField] private RectTransform _iconTransform;
        [SerializeField] private Image _iconImage;
        [SerializeField] private RectTransform _backgroundTransform;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Canvas _sortingCanvas;

        [Header("Active State")]
        [SerializeField] private float _activeScale = 1.18f;
        [SerializeField] private float _activeYOffset = 18f;
        [SerializeField] private Vector2 _activeBackgroundScale = Vector2.one;
        [SerializeField] private float _activeBackgroundYOffset = 3f;
        [SerializeField] private int _activeSortingOrder = 30;
        [SerializeField] private Color _activeIconColor = Color.white;
        [SerializeField] private Color _activeBackgroundColor = Color.white;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _activeBackgroundSprite;

        [Header("Inactive State")]
        [SerializeField] private float _inactiveScale = 1f;
        [SerializeField] private float _inactiveYOffset;
        [SerializeField] private Vector2 _inactiveBackgroundScale = Vector2.one;
        [SerializeField] private float _inactiveBackgroundYOffset;
        [SerializeField] private int _inactiveSortingOrder;
        [SerializeField] private Color _inactiveIconColor = new Color(1f, 1f, 1f, 0.72f);
        [SerializeField] private Color _inactiveBackgroundColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private Sprite _inactiveBackgroundSprite;

        [Header("Availability")]
        [SerializeField] private bool _isComingSoon;

        public event Action<NavbarItem> OnItemClicked;

        private int _pageIndex;
        private bool _selectionSortingEnabled = true;
        private float _currentVisualProgress;
        private Vector3 _scaleVector = Vector3.one;
        private Vector3 _backgroundScaleVector = Vector3.one;
        private Vector2 _initialAnchoredPosition;
        private Vector2 _initialBackgroundAnchoredPosition;

        public int PageIndex => _pageIndex;
        public RectTransform IconTransform => _iconTransform != null ? _iconTransform : transform as RectTransform;
        public bool IsComingSoon => _isComingSoon;

        public void Initialize(int pageIndex)
        {
            _pageIndex = pageIndex;
            if (_iconTransform != null)
            {
                _initialAnchoredPosition = _iconTransform.anchoredPosition;
            }

            if (_backgroundTransform != null)
            {
                _initialBackgroundAnchoredPosition = _backgroundTransform.anchoredPosition;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnItemClicked?.Invoke(this);
        }

        public void SetActiveState(bool active)
        {
            SetVisualProgress(active ? 1f : 0f);
        }

        public void SetSelectionSortingEnabled(bool enabled)
        {
            if (_selectionSortingEnabled == enabled)
            {
                return;
            }

            _selectionSortingEnabled = enabled;
            ApplySortingState(_currentVisualProgress);
        }

        public void SetVisualProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            _currentVisualProgress = progress;

            if (_iconTransform != null)
            {
                var scale = Mathf.Lerp(_inactiveScale, _activeScale, progress);
                _scaleVector.x = scale;
                _scaleVector.y = scale;
                _scaleVector.z = 1f;
                _iconTransform.localScale = _scaleVector;

                var yOffset = Mathf.Lerp(_inactiveYOffset, _activeYOffset, progress);
                _iconTransform.anchoredPosition = new Vector2(_initialAnchoredPosition.x, _initialAnchoredPosition.y + yOffset);
            }

            if (_iconImage != null)
            {
                _iconImage.color = Color.Lerp(_inactiveIconColor, _activeIconColor, progress);
                if (_activeSprite != null && _inactiveSprite != null)
                {
                    _iconImage.sprite = progress > 0.5f ? _activeSprite : _inactiveSprite;
                }
            }

            if (_backgroundImage != null)
            {
                if (_activeBackgroundSprite != null && _inactiveBackgroundSprite != null)
                {
                    _backgroundImage.sprite = progress > 0.5f ? _activeBackgroundSprite : _inactiveBackgroundSprite;
                }

                _backgroundImage.color = Color.Lerp(_inactiveBackgroundColor, _activeBackgroundColor, progress);
            }

            if (_backgroundTransform != null)
            {
                var backgroundScale = Vector2.Lerp(_inactiveBackgroundScale, _activeBackgroundScale, progress);
                _backgroundScaleVector.x = backgroundScale.x;
                _backgroundScaleVector.y = backgroundScale.y;
                _backgroundScaleVector.z = 1f;
                _backgroundTransform.localScale = _backgroundScaleVector;

                var yOffset = Mathf.Lerp(_inactiveBackgroundYOffset, _activeBackgroundYOffset, progress);
                _backgroundTransform.anchoredPosition = new Vector2(
                    _initialBackgroundAnchoredPosition.x,
                    _initialBackgroundAnchoredPosition.y + yOffset);
            }

            ApplySortingState(progress);
        }

        private void ApplySortingState(float progress)
        {
            if (_sortingCanvas == null)
            {
                return;
            }

            var shouldOverrideSorting = _selectionSortingEnabled && progress > 0.001f;
            _sortingCanvas.overrideSorting = shouldOverrideSorting;
            _sortingCanvas.sortingOrder = shouldOverrideSorting ? _activeSortingOrder : _inactiveSortingOrder;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _activeScale = Mathf.Max(0.1f, _activeScale);
            _inactiveScale = Mathf.Max(0.1f, _inactiveScale);
            _activeBackgroundScale.x = Mathf.Max(0.1f, _activeBackgroundScale.x);
            _activeBackgroundScale.y = Mathf.Max(0.1f, _activeBackgroundScale.y);
            _inactiveBackgroundScale.x = Mathf.Max(0.1f, _inactiveBackgroundScale.x);
            _inactiveBackgroundScale.y = Mathf.Max(0.1f, _inactiveBackgroundScale.y);
        }
#endif
    }
}
