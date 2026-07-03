namespace ThreadRace.Presentation.Models
{
    public sealed class ResultPodiumSlotModel
    {
        public ResultPodiumSlotModel(int placement, string displayName, bool isFilled, bool isPlayer)
            : this(placement, displayName, isFilled, isPlayer, string.Empty, string.Empty)
        {
        }

        public ResultPodiumSlotModel(
            int placement,
            string displayName,
            bool isFilled,
            bool isPlayer,
            string rewardDisplayText,
            string rewardIconId)
        {
            Placement = placement;
            DisplayName = displayName;
            IsFilled = isFilled;
            IsPlayer = isPlayer;
            RewardDisplayText = rewardDisplayText ?? string.Empty;
            RewardIconId = rewardIconId ?? string.Empty;
        }

        public int Placement { get; }

        public string DisplayName { get; }

        public bool IsFilled { get; }

        public bool IsPlayer { get; }

        public string RewardDisplayText { get; }

        public string RewardIconId { get; }
    }
}
