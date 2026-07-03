using System;

namespace ThreadRace.Gameplay.Config
{
    public sealed class RewardTierDefinition
    {
        public RewardTierDefinition(
            int rank,
            string rewardId,
            RewardType rewardType,
            int amount,
            string displayText,
            string iconId)
        {
            if (rank <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), "Reward tier rank must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward ID must not be empty.", nameof(rewardId));
            }

            if (!Enum.IsDefined(typeof(RewardType), rewardType))
            {
                throw new ArgumentOutOfRangeException(nameof(rewardType), "Reward type is not supported.");
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Reward amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(displayText))
            {
                throw new ArgumentException("Reward display text must not be empty.", nameof(displayText));
            }

            if (string.IsNullOrWhiteSpace(iconId))
            {
                throw new ArgumentException("Reward icon ID must not be empty.", nameof(iconId));
            }

            Rank = rank;
            RewardId = rewardId;
            RewardType = rewardType;
            Amount = amount;
            DisplayText = displayText;
            IconId = iconId;
        }

        public int Rank { get; }

        public string RewardId { get; }

        public RewardType RewardType { get; }

        public int Amount { get; }

        public string DisplayText { get; }

        public string IconId { get; }
    }
}
