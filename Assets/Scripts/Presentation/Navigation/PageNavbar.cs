using System;
using System.Collections;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ThreadRace.Presentation.Navigation
{
    public sealed class PageNavbar : MonoBehaviour
    {
        [SerializeField] private SwipePageController _pageController;
        [SerializeField] private NavbarItem[] _navbarItems = Array.Empty<NavbarItem>();
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _smoothTransitions = true;
        [SerializeField] private RectTransform _selectionIndicator;
        [SerializeField] private RectTransform _tooltipRoot;
        [SerializeField] private CanvasGroup _tooltipCanvasGroup;
        [SerializeField] private LockedNavbarTooltip _comingSoonTooltip;
        [SerializeField] private TMP_Text _tooltipText;
        [SerializeField] private string _comingSoonMessage = "Coming soon!";

        [Header("Coming Soon Tooltip Alignment")]
        [SerializeField] private Vector2 _leftComingSoonTooltipOffset = new Vector2(34f, 0f);
        [SerializeField] private Vector2 _rightComingSoonTooltipOffset = new Vector2(-34f, 0f);

        public static Action<int, string> OnPageOpened;

        public event Action NavbarItemClicked;

        private int _currentActiveIndex = -1;
        private int _tooltipOpenedFrame = -1;
        private bool _selectionSortingEnabled = true;

        private void Awake()
        {
            InitializeNavbarItems();
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            if (_pageController == null)
            {
                return;
            }

            _pageController.OnPageTransitionProgress += HandleTransitionProgress;
            _pageController.OnPageChanged += HandlePageChanged;
            _pageController.OnBusyStateEnter += HandleBusyEnter;
            _pageController.OnBusyStateExit += HandleBusyExit;
        }

        private void Update()
        {
            if (!IsComingSoonTooltipVisible())
            {
                return;
            }

            if (!TryGetPointerDownPosition(out var screenPoint))
            {
                return;
            }

            if (Time.frameCount == _tooltipOpenedFrame)
            {
                return;
            }

            if (ContainsComingSoonTooltip(screenPoint))
            {
                return;
            }

            HideComingSoonTooltipAnimated();
        }

        private void Start()
        {
            if (_pageController != null)
            {
                SetActiveItemImmediate(_pageController.CurrentPageIndex);
            }

            SetInteractable(true);
            HideComingSoonTooltipImmediate();
            if (_selectionIndicator != null)
            {
                StartCoroutine(InitializeSelectionIndicatorDelayed());
            }
        }

        private void OnDisable()
        {
            HideComingSoonTooltipImmediate();

            if (_pageController == null)
            {
                return;
            }

            _pageController.OnPageTransitionProgress -= HandleTransitionProgress;
            _pageController.OnPageChanged -= HandlePageChanged;
            _pageController.OnBusyStateEnter -= HandleBusyEnter;
            _pageController.OnBusyStateExit -= HandleBusyExit;
        }

        public void SetActiveItemImmediate(int index)
        {
            _currentActiveIndex = index;
            if (_navbarItems == null)
            {
                return;
            }

            for (var i = 0; i < _navbarItems.Length; i++)
            {
                if (_navbarItems[i] != null)
                {
                    _navbarItems[i].SetActiveState(i == index);
                }
            }

            SetSelectionIndicatorImmediate(index);
        }

        public void SetNavbarItems(NavbarItem[] items)
        {
            UnsubscribeNavbarItems();
            _navbarItems = items ?? Array.Empty<NavbarItem>();
            InitializeNavbarItems();
            if (_pageController != null)
            {
                SetActiveItemImmediate(_pageController.CurrentPageIndex);
            }
        }

        public void SetSelectionSortingEnabled(bool enabled)
        {
            if (_selectionSortingEnabled == enabled)
            {
                return;
            }

            _selectionSortingEnabled = enabled;
            ApplySelectionSortingEnabled();
        }

        private void InitializeNavbarItems()
        {
            if (_navbarItems == null)
            {
                return;
            }

            for (var i = 0; i < _navbarItems.Length; i++)
            {
                if (_navbarItems[i] == null)
                {
                    continue;
                }

                _navbarItems[i].Initialize(i);
                _navbarItems[i].SetSelectionSortingEnabled(_selectionSortingEnabled);
                _navbarItems[i].OnItemClicked -= HandleNavbarItemClicked;
                _navbarItems[i].OnItemClicked += HandleNavbarItemClicked;
            }
        }

        private void UnsubscribeNavbarItems()
        {
            if (_navbarItems == null)
            {
                return;
            }

            for (var i = 0; i < _navbarItems.Length; i++)
            {
                if (_navbarItems[i] != null)
                {
                    _navbarItems[i].OnItemClicked -= HandleNavbarItemClicked;
                }
            }
        }

        private void ApplySelectionSortingEnabled()
        {
            if (_navbarItems == null)
            {
                return;
            }

            for (var i = 0; i < _navbarItems.Length; i++)
            {
                if (_navbarItems[i] != null)
                {
                    _navbarItems[i].SetSelectionSortingEnabled(_selectionSortingEnabled);
                }
            }
        }

        private IEnumerator InitializeSelectionIndicatorDelayed()
        {
            yield return new WaitForEndOfFrame();
            if (_pageController != null)
            {
                SetSelectionIndicatorImmediate(_pageController.CurrentPageIndex);
            }
        }

        private void HandleBusyEnter()
        {
            SetInteractable(false);
        }

        private void HandleBusyExit()
        {
            SetInteractable(true);
        }

        private void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        private void HandleNavbarItemClicked(NavbarItem item)
        {
            if (item == null)
            {
                return;
            }

            NavbarItemClicked?.Invoke();

            if (_pageController == null)
            {
                return;
            }

            if (item.IsComingSoon || !_pageController.IsPageIndexAllowed(item.PageIndex))
            {
                ShowComingSoonTooltip(item);
                return;
            }

            HideComingSoonTooltipImmediate();

            if (item.PageIndex == _pageController.CurrentPageIndex)
            {
                return;
            }

            OnPageOpened?.Invoke(item.PageIndex, item.name);
            _pageController.GoToPage(item.PageIndex);
        }

        private void ShowComingSoonTooltip(NavbarItem item)
        {
            if (_tooltipText != null)
            {
                _tooltipText.text = _comingSoonMessage;
            }

            PositionTooltip(item);

            if (_comingSoonTooltip != null)
            {
                _comingSoonTooltip.ShowFrom(item.IconTransform);
                _tooltipOpenedFrame = Time.frameCount;
                return;
            }

            ShowTooltipWithoutAnimation();
        }

        private void PositionTooltip(NavbarItem item)
        {
            var parentRect = _tooltipRoot == null ? null : _tooltipRoot.parent as RectTransform;
            if (_tooltipRoot == null || item == null || parentRect == null)
            {
                return;
            }

            var sourceTransform = item.IconTransform != null ? item.IconTransform : item.transform as RectTransform;
            var sourceWorldPosition = sourceTransform != null ? sourceTransform.position : item.transform.position;
            var localPosition = parentRect.InverseTransformPoint(sourceWorldPosition);
            var offset = ResolveComingSoonTooltipOffset(localPosition.x);
            var halfParentWidth = parentRect.rect.width * 0.5f;
            var halfTooltipWidth = _tooltipRoot.rect.width * 0.5f;
            var clampedX = Mathf.Clamp(localPosition.x + offset.x, -halfParentWidth + halfTooltipWidth, halfParentWidth - halfTooltipWidth);
            var visiblePosition = new Vector2(clampedX, _tooltipRoot.anchoredPosition.y + offset.y);
            _tooltipRoot.anchoredPosition = visiblePosition;
            if (_comingSoonTooltip != null)
            {
                _comingSoonTooltip.SetVisibleAnchoredPosition(visiblePosition);
            }
        }

        private Vector2 ResolveComingSoonTooltipOffset(float sourceLocalX)
        {
            return sourceLocalX < 0f ? _leftComingSoonTooltipOffset : _rightComingSoonTooltipOffset;
        }

        private void HideComingSoonTooltipAnimated()
        {
            if (_comingSoonTooltip != null)
            {
                _comingSoonTooltip.Hide();
            }
            else if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.alpha = 0f;
                _tooltipCanvasGroup.interactable = false;
                _tooltipCanvasGroup.blocksRaycasts = false;
            }

            _tooltipOpenedFrame = -1;
        }

        private void HideComingSoonTooltipImmediate()
        {
            if (_comingSoonTooltip != null)
            {
                _comingSoonTooltip.HideImmediate();
            }

            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.alpha = 0f;
                _tooltipCanvasGroup.interactable = false;
                _tooltipCanvasGroup.blocksRaycasts = false;
            }

            _tooltipOpenedFrame = -1;
        }

        private void ShowTooltipWithoutAnimation()
        {
            if (_tooltipRoot != null && !_tooltipRoot.gameObject.activeSelf)
            {
                _tooltipRoot.gameObject.SetActive(true);
            }

            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.alpha = 1f;
                _tooltipCanvasGroup.interactable = false;
                _tooltipCanvasGroup.blocksRaycasts = false;
            }

            _tooltipOpenedFrame = Time.frameCount;
        }

        private bool IsComingSoonTooltipVisible()
        {
            if (_comingSoonTooltip != null)
            {
                return _comingSoonTooltip.IsVisible;
            }

            return _tooltipCanvasGroup != null && _tooltipCanvasGroup.alpha > 0f;
        }

        private bool ContainsComingSoonTooltip(Vector2 screenPoint)
        {
            if (_comingSoonTooltip != null)
            {
                return _comingSoonTooltip.ContainsScreenPoint(screenPoint);
            }

            if (_tooltipRoot == null || !_tooltipRoot.gameObject.activeInHierarchy)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(_tooltipRoot, screenPoint, null);
        }

        private static bool TryGetPointerDownPosition(out Vector2 screenPoint)
        {
#if ENABLE_INPUT_SYSTEM
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touches = touchscreen.touches;
                for (var i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.wasPressedThisFrame)
                    {
                        screenPoint = touches[i].position.ReadValue();
                        return true;
                    }
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPoint = mouse.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPoint = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPoint = Input.mousePosition;
                return true;
            }
#endif

            screenPoint = default;
            return false;
        }

        private void HandleTransitionProgress(int fromPage, int toPage, float progress)
        {
            if (!_smoothTransitions || _navbarItems == null)
            {
                return;
            }

            if (_pageController != null && _pageController.IsAllowedPageRangeEnabled)
            {
                fromPage = Mathf.Clamp(fromPage, _pageController.MinAllowedPageIndex, _pageController.MaxAllowedPageIndex);
                toPage = Mathf.Clamp(toPage, _pageController.MinAllowedPageIndex, _pageController.MaxAllowedPageIndex);
                if (fromPage == toPage)
                {
                    progress = 1f;
                }
            }

            for (var i = 0; i < _navbarItems.Length; i++)
            {
                if (_navbarItems[i] == null)
                {
                    continue;
                }

                float itemProgress;
                if (i == fromPage && i == toPage)
                {
                    itemProgress = 1f;
                }
                else if (i == fromPage)
                {
                    itemProgress = 1f - progress;
                }
                else if (i == toPage)
                {
                    itemProgress = progress;
                }
                else
                {
                    itemProgress = 0f;
                }

                _navbarItems[i].SetVisualProgress(itemProgress);
            }

            UpdateSelectionIndicatorPosition(fromPage, toPage, progress);
        }

        private void HandlePageChanged(int newPageIndex)
        {
            HideComingSoonTooltipImmediate();
            _currentActiveIndex = newPageIndex;
            if (!_smoothTransitions)
            {
                SetActiveItemImmediate(newPageIndex);
            }

            SetSelectionIndicatorImmediate(newPageIndex);
        }

        private void UpdateSelectionIndicatorPosition(int fromPage, int toPage, float progress)
        {
            if (_selectionIndicator == null || _navbarItems == null ||
                fromPage < 0 || fromPage >= _navbarItems.Length ||
                toPage < 0 || toPage >= _navbarItems.Length)
            {
                return;
            }

            var fromItem = _navbarItems[fromPage];
            var toItem = _navbarItems[toPage];
            if (fromItem == null || toItem == null)
            {
                return;
            }

            var fromWorldPosition = fromItem.transform.position;
            var toWorldPosition = toItem.transform.position;
            var lerpedWorldPosition = Vector3.Lerp(fromWorldPosition, toWorldPosition, progress);
            ApplySelectionIndicatorWorldX(lerpedWorldPosition);
        }

        private void SetSelectionIndicatorImmediate(int index)
        {
            if (_selectionIndicator == null || _navbarItems == null || index < 0 || index >= _navbarItems.Length)
            {
                return;
            }

            var item = _navbarItems[index];
            if (item != null)
            {
                ApplySelectionIndicatorWorldX(item.transform.position);
            }
        }

        private void ApplySelectionIndicatorWorldX(Vector3 worldPosition)
        {
            if (_selectionIndicator.parent == null)
            {
                _selectionIndicator.position = new Vector3(worldPosition.x, _selectionIndicator.position.y, _selectionIndicator.position.z);
                return;
            }

            var localPosition = _selectionIndicator.parent.InverseTransformPoint(worldPosition);
            _selectionIndicator.localPosition = new Vector3(localPosition.x, _selectionIndicator.localPosition.y, _selectionIndicator.localPosition.z);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_comingSoonTooltip == null && _tooltipRoot != null)
            {
                _comingSoonTooltip = _tooltipRoot.GetComponent<LockedNavbarTooltip>();
            }
        }
#endif
    }
}
