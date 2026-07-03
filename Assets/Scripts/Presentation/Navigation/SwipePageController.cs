using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Navigation
{
    [RequireComponent(typeof(ScrollRect))]
    public sealed class SwipePageController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Page Configuration")]
        [SerializeField] private int _pageCount = 5;
        [SerializeField] private bool _startAtMiddlePage = true;
        [SerializeField] private bool _preferLeftMiddle = true;
        [SerializeField] private int _startPageIndex;

        [Header("Allowed Page Range")]
        [SerializeField] private bool _useAllowedPageRange;
        [SerializeField] private int _minAllowedPageIndex;
        [SerializeField] private int _maxAllowedPageIndex = -1;

        [Header("Snap Thresholds")]
        [Range(0.1f, 0.5f)]
        [SerializeField] private float _distanceThreshold = 0.25f;
        [SerializeField] private float _velocityThreshold = 500f;

        [Header("Animation")]
        [SerializeField] private float _snapDuration = 0.35f;
        [SerializeField] private Ease _snapEase = Ease.OutCubic;

        [Header("References")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private PageContentLayout _pageContentLayout;

        public event Action<int, int, float> OnPageTransitionProgress;
        public event Action<int> OnPageChanged;
        public event Action OnBusyStateEnter;
        public event Action OnBusyStateExit;

        private ScrollRect _scrollRect;
        private Tweener _snapTween;
        private int _currentPageIndex;
        private int _targetPageIndex;
        private int _activePointerId = int.MinValue;
        private bool _isDragging;
        private bool _initialized;
        private float _dragStartPositionX;
        private float _dragStartTime;
        private float _viewportWidth;
        private float _viewportHeight;
        private float _minContentX;
        private float _maxContentX;
        private Vector2 _tempAnchoredPosition;

        public int CurrentPageIndex => _currentPageIndex;
        public int PageCount => _pageCount;
        public int MinAllowedPageIndex => GetEffectiveMinAllowedPageIndex();
        public int MaxAllowedPageIndex => GetEffectiveMaxAllowedPageIndex();
        public bool IsAllowedPageRangeEnabled => _useAllowedPageRange;
        public bool IsDragging => _isDragging;
        public bool IsAnimating => _snapTween != null && _snapTween.IsActive() && _snapTween.IsPlaying();
        public bool IsBusy => _isDragging || IsAnimating;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            if (_content == null)
            {
                _content = _scrollRect.content;
            }

            if (_viewport == null)
            {
                _viewport = _scrollRect.viewport;
            }

            ConfigureScrollRect();
        }

        private void Start()
        {
            NormalizeAllowedPageRange();
            _currentPageIndex = CalculateStartPageIndex();
            _targetPageIndex = _currentPageIndex;
        }

        private void Update()
        {
            if (_isDragging || IsAnimating)
            {
                BroadcastTransitionProgress();
            }
        }

        private void LateUpdate()
        {
            DetectViewportChange();
            ClampContentPosition();
        }

        private void OnDestroy()
        {
            KillSnapTween();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_activePointerId != int.MinValue && _activePointerId != eventData.pointerId)
            {
                return;
            }

            if (Mathf.Abs(eventData.delta.x) < Mathf.Abs(eventData.delta.y))
            {
                return;
            }

            KillSnapTween();
            _activePointerId = eventData.pointerId;
            _isDragging = true;
            _dragStartPositionX = _content.anchoredPosition.x;
            _dragStartTime = Time.unscaledTime;
            OnBusyStateEnter?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
            {
                return;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                var expectedX = GetPagePositionX(_currentPageIndex);
                if (Mathf.Abs(_content.anchoredPosition.x - expectedX) > 2f)
                {
                    SnapToPage(GetNearestPageIndex());
                }

                return;
            }

            if (eventData.pointerId != _activePointerId)
            {
                return;
            }

            _isDragging = false;
            _activePointerId = int.MinValue;
            ClampContentPosition();

            var dragDelta = _content.anchoredPosition.x - _dragStartPositionX;
            var dragTime = Time.unscaledTime - _dragStartTime;
            var velocity = dragTime > 0.001f ? dragDelta / dragTime : 0f;
            SnapToPage(DetermineTargetPage(dragDelta, velocity));
        }

        public void GoToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pageCount || !IsPageIndexAllowed(pageIndex))
            {
                return;
            }

            _isDragging = false;
            _activePointerId = int.MinValue;
            KillSnapTween();
            SnapToPage(pageIndex);
        }

        public void SetPageImmediate(int pageIndex)
        {
            pageIndex = ClampToAllowedPageRange(Mathf.Clamp(pageIndex, 0, _pageCount - 1));
            KillSnapTween();
            _isDragging = false;
            _activePointerId = int.MinValue;
            _currentPageIndex = pageIndex;
            _targetPageIndex = pageIndex;

            _tempAnchoredPosition = _content.anchoredPosition;
            _tempAnchoredPosition.x = GetPagePositionX(pageIndex);
            _content.anchoredPosition = _tempAnchoredPosition;

            OnPageTransitionProgress?.Invoke(pageIndex, pageIndex, 1f);
            OnPageChanged?.Invoke(pageIndex);
        }

        public void SetPageCount(int count)
        {
            _pageCount = Mathf.Max(1, count);
            NormalizeAllowedPageRange();
            CacheViewportDimensions();
            SetPageImmediate(CalculateStartPageIndex());
        }

        public bool IsPageIndexAllowed(int pageIndex)
        {
            return pageIndex >= GetEffectiveMinAllowedPageIndex() && pageIndex <= GetEffectiveMaxAllowedPageIndex();
        }

        public void SetAllowedPageRange(int minPageIndex, int maxPageIndex)
        {
            _useAllowedPageRange = true;
            _minAllowedPageIndex = minPageIndex;
            _maxAllowedPageIndex = maxPageIndex;
            NormalizeAllowedPageRange();
            var clampedCurrentPage = ClampToAllowedPageRange(_currentPageIndex);
            _currentPageIndex = clampedCurrentPage;
            _targetPageIndex = clampedCurrentPage;

            if (_initialized)
            {
                CacheViewportDimensions();
                SetPageImmediate(clampedCurrentPage);
            }
        }

        public void ClearAllowedPageRange()
        {
            _useAllowedPageRange = false;
            _minAllowedPageIndex = 0;
            _maxAllowedPageIndex = -1;

            if (_initialized)
            {
                CacheViewportDimensions();
                SetPageImmediate(Mathf.Clamp(_currentPageIndex, 0, _pageCount - 1));
            }
        }

        public void RefreshLayout()
        {
            RelayoutAndAlign();
        }

        private void ConfigureScrollRect()
        {
            _scrollRect.horizontal = true;
            _scrollRect.vertical = false;
            _scrollRect.inertia = false;
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.scrollSensitivity = 1f;
        }

        private void DetectViewportChange()
        {
            if (_isDragging || IsAnimating || _viewport == null || _content == null)
            {
                return;
            }

            var currentWidth = _viewport.rect.width;
            var currentHeight = _viewport.rect.height;
            if (currentWidth < 1f || currentHeight < 1f)
            {
                return;
            }

            if (!_initialized)
            {
                _initialized = true;
                RelayoutAndAlign();
                return;
            }

            if (!Mathf.Approximately(currentWidth, _viewportWidth) || !Mathf.Approximately(currentHeight, _viewportHeight))
            {
                RelayoutAndAlign();
            }
        }

        private void RelayoutAndAlign()
        {
            if (_pageContentLayout != null)
            {
                _pageContentLayout.LayoutPages();
            }

            CacheViewportDimensions();
            SetPageImmediate(_currentPageIndex);
        }

        private void CacheViewportDimensions()
        {
            _viewportWidth = _viewport.rect.width;
            _viewportHeight = _viewport.rect.height;
            _content.sizeDelta = new Vector2(_viewportWidth * _pageCount, _content.sizeDelta.y);
            _maxContentX = GetPagePositionX(GetEffectiveMinAllowedPageIndex());
            _minContentX = GetPagePositionX(GetEffectiveMaxAllowedPageIndex());
        }

        private int CalculateStartPageIndex()
        {
            if (!_startAtMiddlePage)
            {
                return ClampToAllowedPageRange(Mathf.Clamp(_startPageIndex, 0, _pageCount - 1));
            }

            if (_pageCount <= 1)
            {
                return ClampToAllowedPageRange(0);
            }

            if (_pageCount % 2 == 1)
            {
                return ClampToAllowedPageRange(_pageCount / 2);
            }

            return ClampToAllowedPageRange(_preferLeftMiddle ? _pageCount / 2 - 1 : _pageCount / 2);
        }

        private int GetEffectiveMinAllowedPageIndex()
        {
            return _useAllowedPageRange ? Mathf.Clamp(_minAllowedPageIndex, 0, _pageCount - 1) : 0;
        }

        private int GetEffectiveMaxAllowedPageIndex()
        {
            if (!_useAllowedPageRange)
            {
                return _pageCount - 1;
            }

            var maxPage = _maxAllowedPageIndex < 0 ? _pageCount - 1 : _maxAllowedPageIndex;
            maxPage = Mathf.Clamp(maxPage, 0, _pageCount - 1);
            return Mathf.Max(maxPage, GetEffectiveMinAllowedPageIndex());
        }

        private int ClampToAllowedPageRange(int pageIndex)
        {
            return Mathf.Clamp(pageIndex, GetEffectiveMinAllowedPageIndex(), GetEffectiveMaxAllowedPageIndex());
        }

        private void NormalizeAllowedPageRange()
        {
            _pageCount = Mathf.Max(1, _pageCount);
            _minAllowedPageIndex = Mathf.Max(0, _minAllowedPageIndex);
            if (_maxAllowedPageIndex < -1)
            {
                _maxAllowedPageIndex = -1;
            }

            if (_useAllowedPageRange)
            {
                _minAllowedPageIndex = Mathf.Clamp(_minAllowedPageIndex, 0, _pageCount - 1);
                if (_maxAllowedPageIndex >= 0)
                {
                    _maxAllowedPageIndex = Mathf.Clamp(_maxAllowedPageIndex, _minAllowedPageIndex, _pageCount - 1);
                }
            }
        }

        private void ClampContentPosition()
        {
            if (!_initialized || _content == null)
            {
                return;
            }

            var currentX = _content.anchoredPosition.x;
            var clampedX = Mathf.Clamp(currentX, _minContentX, _maxContentX);
            if (Mathf.Approximately(currentX, clampedX))
            {
                return;
            }

            _tempAnchoredPosition = _content.anchoredPosition;
            _tempAnchoredPosition.x = clampedX;
            _content.anchoredPosition = _tempAnchoredPosition;
        }

        private int DetermineTargetPage(float dragDelta, float velocity)
        {
            var dragPercent = Mathf.Abs(dragDelta) / Mathf.Max(1f, _viewportWidth);
            var draggedRight = dragDelta > 0f;
            var swipedFast = Mathf.Abs(velocity) > _velocityThreshold;
            var draggedFar = dragPercent > _distanceThreshold;
            var target = _currentPageIndex;

            if (draggedFar || swipedFast)
            {
                target = draggedRight || swipedFast && velocity > 0f
                    ? _currentPageIndex - 1
                    : _currentPageIndex + 1;
            }
            else
            {
                target = GetNearestPageIndex();
            }

            return ClampToAllowedPageRange(target);
        }

        private int GetNearestPageIndex()
        {
            var normalizedPosition = -_content.anchoredPosition.x / Mathf.Max(1f, _viewportWidth);
            return ClampToAllowedPageRange(Mathf.RoundToInt(normalizedPosition));
        }

        private float GetPagePositionX(int pageIndex)
        {
            return -pageIndex * _viewportWidth;
        }

        private void SnapToPage(int targetPage)
        {
            _targetPageIndex = targetPage;
            var targetX = GetPagePositionX(targetPage);
            KillSnapTween();

            _snapTween = DOTween
                .To(
                    () => _content.anchoredPosition.x,
                    value =>
                    {
                        _tempAnchoredPosition = _content.anchoredPosition;
                        _tempAnchoredPosition.x = value;
                        _content.anchoredPosition = _tempAnchoredPosition;
                    },
                    targetX,
                    _snapDuration)
                .SetEase(_snapEase)
                .SetUpdate(true)
                .OnComplete(OnSnapComplete);
        }

        private void OnSnapComplete()
        {
            var previousPage = _currentPageIndex;
            _currentPageIndex = _targetPageIndex;
            OnPageTransitionProgress?.Invoke(_currentPageIndex, _currentPageIndex, 1f);

            if (previousPage != _currentPageIndex)
            {
                OnPageChanged?.Invoke(_currentPageIndex);
            }

            OnBusyStateExit?.Invoke();
        }

        private void KillSnapTween()
        {
            if (_snapTween != null && _snapTween.IsActive())
            {
                _snapTween.Kill();
            }

            _snapTween = null;
        }

        private void BroadcastTransitionProgress()
        {
            if (_viewportWidth <= 0.0001f)
            {
                return;
            }

            var normalizedPosition = Mathf.Clamp(
                -_content.anchoredPosition.x / _viewportWidth,
                GetEffectiveMinAllowedPageIndex(),
                GetEffectiveMaxAllowedPageIndex());
            var fromPage = Mathf.FloorToInt(normalizedPosition);
            var toPage = Mathf.CeilToInt(normalizedPosition);

            if (fromPage == toPage)
            {
                OnPageTransitionProgress?.Invoke(fromPage, toPage, 1f);
                return;
            }

            OnPageTransitionProgress?.Invoke(fromPage, toPage, normalizedPosition - fromPage);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _pageCount = Mathf.Max(1, _pageCount);
            _startPageIndex = Mathf.Clamp(_startPageIndex, 0, Mathf.Max(0, _pageCount - 1));
            _snapDuration = Mathf.Max(0.05f, _snapDuration);
            NormalizeAllowedPageRange();
        }
#endif
    }
}
