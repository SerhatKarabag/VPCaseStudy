using System;
using DG.Tweening;
using TMPro;
using ThreadRace.Presentation.Animation;
using ThreadRace.Presentation.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _playButtonLabel;
        [SerializeField] private PlayButtonAttentionAnimator _playButtonAttentionAnimator;
        [SerializeField] private PageNavbar _pageNavbar;
        [SerializeField] private Button _threadRaceButton;
        [SerializeField] private TMP_Text _threadRaceCountdownText;
        [SerializeField] private float _fadeDurationSeconds = 0.12f;

        private Tween _visibilityTween;

        public event Action PlayRequested;

        public event Action ThreadRaceRequested;

        public event Action NavigationItemClicked;

        public bool IsVisible { get; private set; }

        public bool IsInteractive => _canvasGroup != null && _canvasGroup.interactable && _canvasGroup.blocksRaycasts;

        public bool IsAvailable => true;

        public void SetVisible(bool visible, bool interactive)
        {
            EnsureReferences();
            IsVisible = visible;

            if (_canvasGroup == null)
            {
                return;
            }

            var isInteractive = visible && interactive;
            _canvasGroup.interactable = isInteractive;
            _canvasGroup.blocksRaycasts = isInteractive;
            _playButtonAttentionAnimator?.SetPlaying(isInteractive);
            _pageNavbar?.SetSelectionSortingEnabled(isInteractive);

            _visibilityTween?.Kill();
            if (!Application.isPlaying || _fadeDurationSeconds <= 0f)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                return;
            }

            _visibilityTween = DOTween
                .To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, visible ? 1f : 0f, _fadeDurationSeconds)
                .SetEase(visible ? Ease.OutQuad : Ease.InQuad)
                .SetUpdate(true);
        }

        public void SetThreadRaceCountdown(string countdownText)
        {
            if (_threadRaceCountdownText != null)
            {
                _threadRaceCountdownText.text = countdownText ?? string.Empty;
            }
        }

        public void SetPlayButtonLabel(string label)
        {
            EnsureReferences();

            if (_playButtonLabel != null)
            {
                _playButtonLabel.text = label ?? string.Empty;
            }
        }

        private void Awake()
        {
            EnsureReferences();
            if (_playButton != null)
            {
                ButtonPressFeedback.Install(_playButton);
                _playButton.onClick.AddListener(OnPlayClicked);
            }

            if (_threadRaceButton != null)
            {
                ButtonPressFeedback.Install(_threadRaceButton);
                _threadRaceButton.onClick.AddListener(OnThreadRaceClicked);
            }

            if (_pageNavbar != null)
            {
                _pageNavbar.NavbarItemClicked += OnNavbarItemClicked;
            }
        }

        private void OnDestroy()
        {
            _visibilityTween?.Kill();
            _visibilityTween = null;

            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (_threadRaceButton != null)
            {
                _threadRaceButton.onClick.RemoveListener(OnThreadRaceClicked);
            }

            if (_pageNavbar != null)
            {
                _pageNavbar.NavbarItemClicked -= OnNavbarItemClicked;
            }
        }

        private void OnPlayClicked()
        {
            PlayRequested?.Invoke();
        }

        private void OnThreadRaceClicked()
        {
            ThreadRaceRequested?.Invoke();
        }

        private void OnNavbarItemClicked()
        {
            NavigationItemClicked?.Invoke();
        }

        private void EnsureReferences()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_playButtonAttentionAnimator == null && _playButton != null)
            {
                _playButtonAttentionAnimator = _playButton.GetComponent<PlayButtonAttentionAnimator>();
            }

            if (_playButtonLabel == null && _playButton != null)
            {
                var labelTransform = _playButton.transform.Find("Label");
                _playButtonLabel = labelTransform == null
                    ? _playButton.GetComponentInChildren<TMP_Text>(true)
                    : labelTransform.GetComponent<TMP_Text>();
            }

            if (_pageNavbar == null)
            {
                _pageNavbar = GetComponentInChildren<PageNavbar>(true);
            }
        }
    }
}
