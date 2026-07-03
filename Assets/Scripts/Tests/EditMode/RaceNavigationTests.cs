using NUnit.Framework;
using System.Reflection;
using ThreadRace.Presentation.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RaceNavigationTests
    {
        [Test]
        public void PageContentLayout_StretchAnchoredContentKeepsViewportHeight()
        {
            var viewportObject = new GameObject("Viewport", typeof(RectTransform));
            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(PageContentLayout));

            try
            {
                var viewport = viewportObject.GetComponent<RectTransform>();
                viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1080f);
                viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1730f);

                var content = contentObject.GetComponent<RectTransform>();
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);

                for (var i = 0; i < 5; i++)
                {
                    var pageObject = new GameObject("Page" + i.ToString(), typeof(RectTransform));
                    pageObject.transform.SetParent(content, false);
                }

                var layout = contentObject.GetComponent<PageContentLayout>();
                layout.SetViewport(viewport);
                layout.LayoutPages();

                Assert.AreEqual(1080f * 5f, content.rect.width, 0.01f);
                Assert.AreEqual(1730f, content.rect.height, 0.01f);

                for (var i = 0; i < content.childCount; i++)
                {
                    var page = (RectTransform)content.GetChild(i);
                    Assert.AreEqual(1080f, page.rect.width, 0.01f);
                    Assert.AreEqual(1730f, page.rect.height, 0.01f);
                    Assert.AreEqual(i * 1080f, page.anchoredPosition.x, 0.01f);
                }
            }
            finally
            {
                Object.DestroyImmediate(contentObject);
                Object.DestroyImmediate(viewportObject);
            }
        }

        [Test]
        public void LockedNavbarTooltip_MirrorsOnlyBackgroundForLeftSource()
        {
            var parentObject = new GameObject("Parent", typeof(RectTransform), typeof(Canvas));
            var tooltipObject = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(LockedNavbarTooltip));
            var backgroundObject = new GameObject("Background", typeof(RectTransform));
            var labelObject = new GameObject("Label", typeof(RectTransform));
            var sourceObject = new GameObject("Source", typeof(RectTransform));

            try
            {
                var parent = parentObject.GetComponent<RectTransform>();
                parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 540f);
                parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 180f);

                var tooltipRoot = tooltipObject.GetComponent<RectTransform>();
                tooltipRoot.SetParent(parent, false);
                tooltipRoot.sizeDelta = new Vector2(308.7f, 145.7f);

                var background = backgroundObject.GetComponent<RectTransform>();
                background.SetParent(tooltipRoot, false);
                background.localScale = Vector3.one;

                var label = labelObject.GetComponent<RectTransform>();
                label.SetParent(tooltipRoot, false);
                label.localScale = Vector3.one;

                var source = sourceObject.GetComponent<RectTransform>();
                source.SetParent(parent, false);
                source.sizeDelta = new Vector2(92f, 92f);

                var tooltip = tooltipObject.GetComponent<LockedNavbarTooltip>();
                var serialized = new SerializedObject(tooltip);
                serialized.FindProperty("_root").objectReferenceValue = tooltipRoot;
                serialized.FindProperty("_canvasGroup").objectReferenceValue = tooltipObject.GetComponent<CanvasGroup>();
                serialized.FindProperty("_backgroundRoot").objectReferenceValue = background;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                tooltip.SetVisibleAnchoredPosition(Vector2.zero);

                source.anchoredPosition = new Vector2(-220f, 0f);
                tooltip.ShowFrom(source);
                Assert.Less(background.localScale.x, 0f);
                Assert.AreEqual(1f, label.localScale.x);

                tooltip.HideImmediate();

                source.anchoredPosition = new Vector2(220f, 0f);
                tooltip.ShowFrom(source);
                Assert.Greater(background.localScale.x, 0f);
                Assert.AreEqual(1f, label.localScale.x);

                tooltip.HideImmediate();
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(labelObject);
                Object.DestroyImmediate(backgroundObject);
                Object.DestroyImmediate(tooltipObject);
                Object.DestroyImmediate(parentObject);
            }
        }

        [Test]
        public void PageNavbar_ComingSoonTooltipOffsetsAreAppliedPerSide()
        {
            var navbarObject = new GameObject("Navbar", typeof(RectTransform), typeof(PageNavbar));
            var tooltipObject = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasGroup));
            var leftItemObject = new GameObject("LeftItem", typeof(RectTransform), typeof(NavbarItem));
            var rightItemObject = new GameObject("RightItem", typeof(RectTransform), typeof(NavbarItem));

            try
            {
                var navbarRect = navbarObject.GetComponent<RectTransform>();
                navbarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 540f);
                navbarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 180f);

                var tooltipRoot = tooltipObject.GetComponent<RectTransform>();
                tooltipRoot.SetParent(navbarRect, false);
                tooltipRoot.sizeDelta = new Vector2(120f, 40f);
                tooltipRoot.anchoredPosition = new Vector2(0f, 220f);

                var leftItem = leftItemObject.GetComponent<NavbarItem>();
                var leftRect = leftItemObject.GetComponent<RectTransform>();
                leftRect.SetParent(navbarRect, false);
                leftRect.anchoredPosition = new Vector2(-100f, 0f);

                var rightItem = rightItemObject.GetComponent<NavbarItem>();
                var rightRect = rightItemObject.GetComponent<RectTransform>();
                rightRect.SetParent(navbarRect, false);
                rightRect.anchoredPosition = new Vector2(100f, 0f);

                var pageNavbar = navbarObject.GetComponent<PageNavbar>();
                var serialized = new SerializedObject(pageNavbar);
                serialized.FindProperty("_tooltipRoot").objectReferenceValue = tooltipRoot;
                serialized.FindProperty("_leftComingSoonTooltipOffset").vector2Value = new Vector2(18f, 3f);
                serialized.FindProperty("_rightComingSoonTooltipOffset").vector2Value = new Vector2(-22f, -4f);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                InvokePositionTooltip(pageNavbar, leftItem);
                AssertVector2(new Vector2(-82f, 223f), tooltipRoot.anchoredPosition);

                tooltipRoot.anchoredPosition = new Vector2(0f, 220f);
                InvokePositionTooltip(pageNavbar, rightItem);
                AssertVector2(new Vector2(78f, 216f), tooltipRoot.anchoredPosition);
            }
            finally
            {
                Object.DestroyImmediate(rightItemObject);
                Object.DestroyImmediate(leftItemObject);
                Object.DestroyImmediate(tooltipObject);
                Object.DestroyImmediate(navbarObject);
            }
        }

        private static void InvokePositionTooltip(PageNavbar pageNavbar, NavbarItem item)
        {
            var method = typeof(PageNavbar).GetMethod("PositionTooltip", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(pageNavbar, new object[] { item });
        }

        private static void AssertVector2(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.01f);
            Assert.AreEqual(expected.y, actual.y, 0.01f);
        }
    }
}
