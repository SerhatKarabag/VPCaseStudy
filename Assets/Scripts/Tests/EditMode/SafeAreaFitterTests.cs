using NUnit.Framework;
using ThreadRace.Presentation.Views;
using UnityEngine;

namespace ThreadRace.Tests.EditMode
{
    public sealed class SafeAreaFitterTests
    {
        [Test]
        public void KnownSafeAreaProducesExpectedAnchors()
        {
            SafeAreaFitter.CalculateAnchors(
                new Rect(0f, 100f, 1080f, 1720f),
                1080,
                1920,
                out var anchorMin,
                out var anchorMax);

            Assert.AreEqual(new Vector2(0f, 100f / 1920f), anchorMin);
            Assert.AreEqual(new Vector2(1f, 1820f / 1920f), anchorMax);
        }

        [Test]
        public void FullScreenSafeAreaProducesFullAnchors()
        {
            SafeAreaFitter.CalculateAnchors(new Rect(0f, 0f, 1080f, 1920f), 1080, 1920, out var anchorMin, out var anchorMax);

            Assert.AreEqual(Vector2.zero, anchorMin);
            Assert.AreEqual(Vector2.one, anchorMax);
        }

        [Test]
        public void InvalidScreenValuesFallbackToFullAnchors()
        {
            SafeAreaFitter.CalculateAnchors(new Rect(0f, 0f, 0f, 0f), 0, 0, out var anchorMin, out var anchorMax);

            Assert.AreEqual(Vector2.zero, anchorMin);
            Assert.AreEqual(Vector2.one, anchorMax);
        }
    }
}
