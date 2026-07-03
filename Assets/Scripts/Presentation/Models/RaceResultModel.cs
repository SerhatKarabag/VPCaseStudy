using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThreadRace.Gameplay.Config;

namespace ThreadRace.Presentation.Models
{
    public sealed class RaceResultModel
    {
        private readonly ReadOnlyCollection<ResultPodiumSlotModel> _podiumSlots;

        public RaceResultModel(
            string title,
            string playerPlacementText,
            string rewardStatusText,
            bool rewardEligible,
            IEnumerable<ResultPodiumSlotModel> podiumSlots)
            : this(
                title,
                playerPlacementText,
                rewardStatusText,
                rewardEligible,
                string.Empty,
                RewardType.Custom,
                0,
                string.Empty,
                string.Empty,
                false,
                podiumSlots)
        {
        }

        public RaceResultModel(
            string title,
            string playerPlacementText,
            string rewardStatusText,
            bool rewardEligible,
            string rewardId,
            RewardType rewardType,
            int rewardAmount,
            string rewardDisplayText,
            string rewardIconId,
            bool rewardClaimed,
            IEnumerable<ResultPodiumSlotModel> podiumSlots)
        {
            Title = title;
            PlayerPlacementText = playerPlacementText;
            RewardStatusText = rewardStatusText;
            RewardEligible = rewardEligible;
            RewardId = rewardId ?? string.Empty;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
            RewardDisplayText = rewardDisplayText ?? string.Empty;
            RewardIconId = rewardIconId ?? string.Empty;
            RewardClaimed = rewardClaimed;

            if (podiumSlots == null)
            {
                throw new ArgumentNullException(nameof(podiumSlots));
            }

            var copied = new List<ResultPodiumSlotModel>();
            foreach (var podiumSlot in podiumSlots)
            {
                if (podiumSlot == null)
                {
                    throw new ArgumentException("Podium slots must not contain null entries.", nameof(podiumSlots));
                }

                copied.Add(podiumSlot);
            }

            _podiumSlots = Array.AsReadOnly(copied.ToArray());
        }

        public string Title { get; }

        public string PlayerPlacementText { get; }

        public string RewardStatusText { get; }

        public bool RewardEligible { get; }

        public string RewardId { get; }

        public RewardType RewardType { get; }

        public int RewardAmount { get; }

        public string RewardDisplayText { get; }

        public string RewardIconId { get; }

        public bool RewardClaimed { get; }

        public IReadOnlyList<ResultPodiumSlotModel> PodiumSlots => _podiumSlots;
    }
}
