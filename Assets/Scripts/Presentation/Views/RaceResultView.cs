using System;
using TMPro;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    public sealed class RaceResultView : PhaseViewBase, IRaceResultView
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _playerPlacementText;
        [SerializeField] private TMP_Text _rewardStatusText;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private RewardIconBinding[] _rewardIcons;
        [SerializeField] private ResultPodiumSlotView[] _podiumSlots;
        [SerializeField] private Button _continueButton;

        public event Action ContinueRequested;

        public void Render(RaceResultModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (_titleText != null)
            {
                _titleText.text = model.Title;
            }

            if (_playerPlacementText != null)
            {
                _playerPlacementText.text = model.PlayerPlacementText;
            }

            if (_rewardStatusText != null)
            {
                _rewardStatusText.text = model.RewardStatusText;
            }

            if (_rewardIcon != null)
            {
                var icon = FindRewardIcon(model.RewardIconId);
                _rewardIcon.sprite = icon;
                _rewardIcon.enabled = icon != null;
            }

            if (_podiumSlots != null)
            {
                var count = Math.Min(_podiumSlots.Length, model.PodiumSlots.Count);
                for (var i = 0; i < count; i++)
                {
                    if (_podiumSlots[i] != null)
                    {
                        _podiumSlots[i].Render(model.PodiumSlots[i]);
                    }
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (_continueButton != null)
            {
                ButtonPressFeedback.Install(_continueButton);
                _continueButton.onClick.AddListener(OnContinueButtonClicked);
            }
        }

        protected override void OnDestroy()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            }

            base.OnDestroy();
        }

        private void OnContinueButtonClicked()
        {
            PlayScalePulse();
            ContinueRequested?.Invoke();
        }

        private Sprite FindRewardIcon(string iconId)
        {
            if (string.IsNullOrWhiteSpace(iconId) || _rewardIcons == null)
            {
                return null;
            }

            for (var i = 0; i < _rewardIcons.Length; i++)
            {
                if (_rewardIcons[i] != null && _rewardIcons[i].Matches(iconId))
                {
                    return _rewardIcons[i].Sprite;
                }
            }

            return null;
        }

        [Serializable]
        private sealed class RewardIconBinding
        {
            [SerializeField] private string _iconId;
            [SerializeField] private Sprite _sprite;

            public Sprite Sprite => _sprite;

            public bool Matches(string iconId)
            {
                return string.Equals(_iconId, iconId, StringComparison.Ordinal);
            }
        }
    }
}
