using System;
using DG.Tweening;
using ThreadRace.Presentation.Views;
using UnityEngine;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RacePresentationWarmup : IInitializable
    {
        private const int TweenCapacity = 256;
        private const int SequenceCapacity = 64;

        private readonly IEntryPopupView _entryPopupView;
        private readonly IRaceHudView _raceHudView;
        private readonly IPlaceholderLevelView _placeholderLevelView;
        private readonly IRaceResultView _raceResultView;

        public RacePresentationWarmup(
            IEntryPopupView entryPopupView,
            IRaceHudView raceHudView,
            IPlaceholderLevelView placeholderLevelView,
            IRaceResultView raceResultView)
        {
            _entryPopupView = entryPopupView ?? throw new ArgumentNullException(nameof(entryPopupView));
            _raceHudView = raceHudView ?? throw new ArgumentNullException(nameof(raceHudView));
            _placeholderLevelView = placeholderLevelView ?? throw new ArgumentNullException(nameof(placeholderLevelView));
            _raceResultView = raceResultView ?? throw new ArgumentNullException(nameof(raceResultView));
        }

        public void Initialize()
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

            if (DOTween.TotalActiveTweens() == 0)
            {
                DOTween.SetTweensCapacity(TweenCapacity, SequenceCapacity);
            }

            WarmUp(_entryPopupView);
            WarmUp(_raceHudView);
            WarmUp(_placeholderLevelView);
            WarmUp(_raceResultView);
            Canvas.ForceUpdateCanvases();
        }

        private static void WarmUp(IPhaseView view)
        {
            if (view is PhaseViewBase phaseView)
            {
                phaseView.WarmUpForFirstShow();
            }
        }
    }
}
