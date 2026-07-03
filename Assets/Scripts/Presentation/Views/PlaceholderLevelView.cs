using System;
using DG.Tweening;
using TMPro;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    public sealed class PlaceholderLevelView : PhaseViewBase, IPlaceholderLevelView
    {
        [SerializeField] private CanvasGroup _challengeGroup;
        [SerializeField] private CanvasGroup _levelWinGroup;
        [SerializeField] private CanvasGroup _levelFailGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private TMP_Text _coinRewardText;
        [SerializeField] private Button _successButton;
        [SerializeField] private Button _failButton;
        [SerializeField] private Button _levelWinClaimButton;
        [SerializeField] private Button _levelFailReturnButton;
        [SerializeField] private float _screenFadeDurationSeconds = 0.14f;
        [SerializeField] private float _screenSpringDurationSeconds = 0.32f;
        [SerializeField] private float _screenSpringStartScale = 0.88f;
        [SerializeField] private Ease _screenSpringEase = Ease.OutBack;

        private Tween _challengeTween;
        private Tween _levelWinTween;
        private Tween _levelFailTween;
        private bool _hasActiveScreen;
        private PlaceholderLevelScreen _activeScreen;

        public event Action SuccessRequested;

        public event Action FailRequested;

        public event Action LevelWinClaimRequested;

        public event Action LevelFailReturnRequested;

        public void Render(PlaceholderLevelModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            SetScreen(model.Screen);

            if (_titleText != null)
            {
                _titleText.text = model.Title;
            }

            if (_instructionText != null)
            {
                _instructionText.text = model.Instruction;
            }

            if (_coinRewardText != null)
            {
                _coinRewardText.text = model.CoinRewardText;
            }

            SetButtonsEnabled(model.ButtonsEnabled && model.Screen == PlaceholderLevelScreen.Challenge);
            SetButtonEnabled(_levelWinClaimButton, model.Screen == PlaceholderLevelScreen.LevelWin);
            SetButtonEnabled(_levelFailReturnButton, model.Screen == PlaceholderLevelScreen.LevelFail);
        }

        protected override void Awake()
        {
            base.Awake();
            if (_successButton != null)
            {
                ButtonPressFeedback.Install(_successButton);
                _successButton.onClick.AddListener(OnSuccessButtonClicked);
            }

            if (_failButton != null)
            {
                ButtonPressFeedback.Install(_failButton);
                _failButton.onClick.AddListener(OnFailButtonClicked);
            }

            if (_levelWinClaimButton != null)
            {
                ButtonPressFeedback.Install(_levelWinClaimButton);
                _levelWinClaimButton.onClick.AddListener(OnLevelWinClaimClicked);
            }

            if (_levelFailReturnButton != null)
            {
                ButtonPressFeedback.Install(_levelFailReturnButton);
                _levelFailReturnButton.onClick.AddListener(OnLevelFailReturnClicked);
            }
        }

        protected override void OnDestroy()
        {
            KillScreenTweens();

            if (_successButton != null)
            {
                _successButton.onClick.RemoveListener(OnSuccessButtonClicked);
            }

            if (_failButton != null)
            {
                _failButton.onClick.RemoveListener(OnFailButtonClicked);
            }

            if (_levelWinClaimButton != null)
            {
                _levelWinClaimButton.onClick.RemoveListener(OnLevelWinClaimClicked);
            }

            if (_levelFailReturnButton != null)
            {
                _levelFailReturnButton.onClick.RemoveListener(OnLevelFailReturnClicked);
            }

            base.OnDestroy();
        }

        private void SetScreen(PlaceholderLevelScreen screen)
        {
            var screenChanged = !_hasActiveScreen || _activeScreen != screen;
            _hasActiveScreen = true;
            _activeScreen = screen;

            SetGroupVisible(_challengeGroup, screen == PlaceholderLevelScreen.Challenge, screenChanged, ref _challengeTween);
            SetGroupVisible(_levelWinGroup, screen == PlaceholderLevelScreen.LevelWin, screenChanged, ref _levelWinTween);
            SetGroupVisible(_levelFailGroup, screen == PlaceholderLevelScreen.LevelFail, screenChanged, ref _levelFailTween);
        }

        private void SetGroupVisible(CanvasGroup group, bool visible, bool animateShow, ref Tween tween)
        {
            if (group == null)
            {
                return;
            }

            if (visible && !animateShow && Application.isPlaying &&
                ((tween != null && tween.IsActive()) || group.alpha > 0f))
            {
                return;
            }

            tween?.Kill();
            tween = null;

            if (!visible || !Application.isPlaying || !animateShow)
            {
                ApplyGroupVisibility(group, visible);
                return;
            }

            if (!IsVisible)
            {
                ApplyGroupVisibility(group, true);
                return;
            }

            var target = group.transform as RectTransform;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            if (target != null)
            {
                target.localScale = Vector3.one * Mathf.Clamp(_screenSpringStartScale, 0.1f, 1f);
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Insert(0f, DOTween
                .To(() => group.alpha, value => group.alpha = value, 1f, Mathf.Max(0f, _screenFadeDurationSeconds))
                .SetEase(Ease.OutQuad));

            if (target != null)
            {
                sequence.Insert(0f, DOTween
                    .To(() => target.localScale, value => target.localScale = value, Vector3.one, Mathf.Max(0f, _screenSpringDurationSeconds))
                    .SetEase(_screenSpringEase));
            }

            tween = sequence.OnComplete(() =>
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;

                if (target != null)
                {
                    target.localScale = Vector3.one;
                }
            });
        }

        private static void ApplyGroupVisibility(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;

            var target = group.transform as RectTransform;
            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        private void KillScreenTweens()
        {
            _challengeTween?.Kill();
            _challengeTween = null;
            _levelWinTween?.Kill();
            _levelWinTween = null;
            _levelFailTween?.Kill();
            _levelFailTween = null;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            SetButtonEnabled(_successButton, enabled);
            SetButtonEnabled(_failButton, enabled);
        }

        private static void SetButtonEnabled(Button button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }

        private void OnSuccessButtonClicked()
        {
            PlayScalePulse();
            SuccessRequested?.Invoke();
        }

        private void OnFailButtonClicked()
        {
            PlayScalePulse();
            FailRequested?.Invoke();
        }

        private void OnLevelWinClaimClicked()
        {
            PlayScalePulse();
            LevelWinClaimRequested?.Invoke();
        }

        private void OnLevelFailReturnClicked()
        {
            PlayScalePulse();
            LevelFailReturnRequested?.Invoke();
        }
    }
}
