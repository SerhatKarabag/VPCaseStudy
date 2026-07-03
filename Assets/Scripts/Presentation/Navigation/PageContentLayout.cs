using UnityEngine;

namespace ThreadRace.Presentation.Navigation
{
    public sealed class PageContentLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private bool _layoutOnStart = true;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (_layoutOnStart)
            {
                LayoutPages();
            }
        }

        [ContextMenu("Layout Pages")]
        public void LayoutPages()
        {
            EnsureReferences();
            if (_viewport == null || _rectTransform == null)
            {
                return;
            }

            var viewportWidth = _viewport.rect.width;
            var viewportHeight = _viewport.rect.height;
            if (viewportWidth <= 0f || viewportHeight <= 0f)
            {
                return;
            }

            var pageCount = _rectTransform.childCount;
            _rectTransform.anchorMin = new Vector2(0f, 0f);
            _rectTransform.anchorMax = new Vector2(0f, 0f);
            _rectTransform.pivot = new Vector2(0f, 0f);
            _rectTransform.sizeDelta = new Vector2(viewportWidth * pageCount, viewportHeight);
            _rectTransform.anchoredPosition = Vector2.zero;

            for (var i = 0; i < pageCount; i++)
            {
                if (!(_rectTransform.GetChild(i) is RectTransform page))
                {
                    continue;
                }

                page.anchorMin = new Vector2(0f, 0f);
                page.anchorMax = new Vector2(0f, 1f);
                page.pivot = new Vector2(0f, 0.5f);
                page.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);
                page.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportHeight);
                page.anchoredPosition = new Vector2(i * viewportWidth, 0f);
            }
        }

        public void SetViewport(RectTransform viewport)
        {
            _viewport = viewport;
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }
    }
}
