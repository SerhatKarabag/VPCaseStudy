using UnityEngine;

namespace ThreadRace.Presentation.Views
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;

        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            if (_target == null)
            {
                _target = transform as RectTransform;
            }

            ApplyIfChanged(force: true);
        }

        private void Update()
        {
            ApplyIfChanged(force: false);
        }

        public static void CalculateAnchors(Rect safeArea, int screenWidth, int screenHeight, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || safeArea.width <= 0f || safeArea.height <= 0f)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            anchorMin = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenWidth),
                Mathf.Clamp01(safeArea.yMin / screenHeight));
            anchorMax = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenWidth),
                Mathf.Clamp01(safeArea.yMax / screenHeight));
        }

        private void ApplyIfChanged(bool force)
        {
            var safeArea = Screen.safeArea;
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (!force
                && _lastSafeArea == safeArea
                && _lastScreenWidth == screenWidth
                && _lastScreenHeight == screenHeight)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;

            if (_target == null)
            {
                return;
            }

            CalculateAnchors(safeArea, screenWidth, screenHeight, out var anchorMin, out var anchorMax);
            _target.anchorMin = anchorMin;
            _target.anchorMax = anchorMax;
            _target.offsetMin = Vector2.zero;
            _target.offsetMax = Vector2.zero;
        }
    }
}
