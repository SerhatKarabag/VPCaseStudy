using TMPro;
using ThreadRace.Presentation.Models;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    public sealed class ResultPodiumSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _placementText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _rewardText;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private RewardIconBinding[] _rewardIcons;
        [SerializeField] private Image _playerAccent;

        public void Render(ResultPodiumSlotModel model)
        {
            if (model == null)
            {
                throw new System.ArgumentNullException(nameof(model));
            }

            if (_placementText != null)
            {
                _placementText.text = "#" + model.Placement.ToString();
            }

            if (_nameText != null)
            {
                _nameText.text = model.IsFilled ? model.DisplayName : "-";
            }

            if (_rewardText != null)
            {
                _rewardText.text = model.RewardDisplayText;
            }

            if (_rewardIcon != null)
            {
                var icon = FindRewardIcon(model.RewardIconId);
                _rewardIcon.sprite = icon;
                _rewardIcon.enabled = icon != null;
            }

            if (_playerAccent != null)
            {
                _playerAccent.enabled = model.IsPlayer;
            }
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

        [System.Serializable]
        private sealed class RewardIconBinding
        {
            [SerializeField] private string _iconId;
            [SerializeField] private Sprite _sprite;

            public Sprite Sprite => _sprite;

            public bool Matches(string iconId)
            {
                return string.Equals(_iconId, iconId, System.StringComparison.Ordinal);
            }
        }
    }
}
