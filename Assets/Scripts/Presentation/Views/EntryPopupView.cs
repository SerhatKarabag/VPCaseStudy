using System;
using TMPro;
using ThreadRace.Presentation.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    public sealed class EntryPopupView : PhaseViewBase, IEntryPopupView
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _ruleText;
        [SerializeField] private TMP_Text _durationText;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeButton;

        public event Action StartRequested;

        public event Action CloseRequested;

        public void SetStartEnabled(bool enabled)
        {
            if (_startButton != null)
            {
                _startButton.interactable = enabled;
            }
        }

        public void SetContent(string title, string body, string rule, string durationLine)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_bodyText != null)
            {
                _bodyText.text = body;
            }

            if (_ruleText != null)
            {
                _ruleText.text = rule;
            }

            if (_durationText != null)
            {
                _durationText.text = durationLine;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (_startButton != null)
            {
                ButtonPressFeedback.Install(_startButton);
                _startButton.onClick.AddListener(OnStartButtonClicked);
            }

            if (_closeButton != null)
            {
                ButtonPressFeedback.Install(_closeButton);
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        protected override void OnDestroy()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(OnStartButtonClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            base.OnDestroy();
        }

        private void OnStartButtonClicked()
        {
            PlayScalePulse();
            StartRequested?.Invoke();
        }

        private void OnCloseButtonClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
