using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Config
{
    public sealed class RaceConfiguration
    {
        public const int DefaultRacerCount = 5;
        public const int RequiredPlayerCount = 1;
        public const int MinimumAiCount = 1;
        public const int MinimumRacerCount = RequiredPlayerCount + MinimumAiCount;
        public const int DefaultFinishTarget = 10;
        public const int DefaultRewardedPositionCount = 3;

        private readonly RacerDefinition[] _racers;
        private readonly ReadOnlyCollection<RacerDefinition> _racersView;
        private readonly RewardTierDefinition[] _rewardTiers;
        private readonly ReadOnlyCollection<RewardTierDefinition> _rewardTiersView;

        public RaceConfiguration(
            IEnumerable<RacerDefinition> racers,
            int finishTarget,
            IEnumerable<RewardTierDefinition> rewardTiers)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            if (finishTarget <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finishTarget), "Finish target must be greater than zero.");
            }

            var racerList = CopyRacers(racers);
            var rewardTierList = CopyRewardTiers(rewardTiers);

            if (racerList.Count < MinimumRacerCount)
            {
                throw new ArgumentException($"Race configuration must contain at least {MinimumRacerCount} racers.", nameof(racers));
            }

            if (rewardTierList.Count <= 0)
            {
                throw new ArgumentException("Race configuration must contain at least one reward tier.", nameof(rewardTiers));
            }

            if (rewardTierList.Count > racerList.Count)
            {
                throw new ArgumentException("Reward tier count must not exceed racer count.", nameof(rewardTiers));
            }

            ValidateRacers(racerList);
            ValidateRewardTiers(rewardTierList);

            _racers = racerList.ToArray();
            Array.Sort(_racers, CompareInitialOrder);
            _racersView = Array.AsReadOnly(_racers);
            _rewardTiers = rewardTierList.ToArray();
            Array.Sort(_rewardTiers, CompareRewardRank);
            _rewardTiersView = Array.AsReadOnly(_rewardTiers);
            FinishTarget = finishTarget;
            RewardedPositionCount = _rewardTiers.Length;
            PlayerRacerIndex = FindPlayerRacerIndex(_racers);
        }

        public int FinishTarget { get; }

        public int RewardedPositionCount { get; }

        public int PlayerRacerIndex { get; }

        public IReadOnlyList<RacerDefinition> Racers => _racersView;

        public IReadOnlyList<RewardTierDefinition> RewardTiers => _rewardTiersView;

        public RacerDefinition PlayerDefinition => _racers[PlayerRacerIndex];

        public RacerDefinition GetRacer(int index)
        {
            if (index < 0 || index >= _racers.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _racers[index];
        }

        public int GetRacerIndex(RacerId racerId)
        {
            for (var i = 0; i < _racers.Length; i++)
            {
                if (_racers[i].Id == racerId)
                {
                    return i;
                }
            }

            return -1;
        }

        public RewardTierDefinition GetRewardTierForRank(int rank)
        {
            for (var i = 0; i < _rewardTiers.Length; i++)
            {
                if (_rewardTiers[i].Rank == rank)
                {
                    return _rewardTiers[i];
                }
            }

            return null;
        }

        private static List<RacerDefinition> CopyRacers(IEnumerable<RacerDefinition> racers)
        {
            var copied = new List<RacerDefinition>(DefaultRacerCount);

            foreach (var racer in racers)
            {
                if (racer == null)
                {
                    throw new ArgumentException("Race configuration must not contain null racer definitions.", nameof(racers));
                }

                copied.Add(racer);
            }

            return copied;
        }

        private static List<RewardTierDefinition> CopyRewardTiers(IEnumerable<RewardTierDefinition> rewardTiers)
        {
            if (rewardTiers == null)
            {
                throw new ArgumentNullException(nameof(rewardTiers));
            }

            var copied = new List<RewardTierDefinition>(DefaultRewardedPositionCount);
            foreach (var rewardTier in rewardTiers)
            {
                if (rewardTier == null)
                {
                    throw new ArgumentException("Reward tiers must not contain null entries.", nameof(rewardTiers));
                }

                copied.Add(rewardTier);
            }

            return copied;
        }

        private static void ValidateRacers(IReadOnlyList<RacerDefinition> racers)
        {
            var ids = new HashSet<RacerId>();
            var initialOrders = new HashSet<int>();
            var playerCount = 0;
            var aiCount = 0;

            for (var i = 0; i < racers.Count; i++)
            {
                var racer = racers[i];

                if (!racer.Id.IsValid)
                {
                    throw new ArgumentException("Race configuration contains an empty racer ID.", nameof(racers));
                }

                if (!ids.Add(racer.Id))
                {
                    throw new ArgumentException($"Duplicate racer ID '{racer.Id}' is not allowed.", nameof(racers));
                }

                if (string.IsNullOrWhiteSpace(racer.DisplayName))
                {
                    throw new ArgumentException($"Racer '{racer.Id}' has an empty display name.", nameof(racers));
                }

                if (!initialOrders.Add(racer.InitialOrder))
                {
                    throw new ArgumentException($"Duplicate initial order '{racer.InitialOrder}' is not allowed.", nameof(racers));
                }

                if (racer.RacerType == RacerType.Player)
                {
                    playerCount++;
                }
                else if (racer.RacerType == RacerType.Ai)
                {
                    aiCount++;
                    if (racer.AiStepTiming == null)
                    {
                        throw new ArgumentException($"AI racer '{racer.Id}' is missing step timing.", nameof(racers));
                    }
                }
                else
                {
                    throw new ArgumentException($"Racer '{racer.Id}' has unsupported racer type.", nameof(racers));
                }
            }

            if (playerCount != RequiredPlayerCount)
            {
                throw new ArgumentException($"Race configuration must contain exactly {RequiredPlayerCount} player racer.", nameof(racers));
            }

            if (aiCount < MinimumAiCount)
            {
                throw new ArgumentException($"Race configuration must contain at least {MinimumAiCount} AI racer.", nameof(racers));
            }
        }

        private static void ValidateRewardTiers(IReadOnlyList<RewardTierDefinition> rewardTiers)
        {
            var ranks = new HashSet<int>();

            for (var i = 0; i < rewardTiers.Count; i++)
            {
                var rewardTier = rewardTiers[i];
                if (!ranks.Add(rewardTier.Rank))
                {
                    throw new ArgumentException($"Duplicate reward tier rank '{rewardTier.Rank}' is not allowed.", nameof(rewardTiers));
                }
            }

            for (var expectedRank = 1; expectedRank <= rewardTiers.Count; expectedRank++)
            {
                if (!ranks.Contains(expectedRank))
                {
                    throw new ArgumentException("Reward tier ranks must be contiguous from rank 1.", nameof(rewardTiers));
                }
            }
        }

        private static int FindPlayerRacerIndex(IReadOnlyList<RacerDefinition> racers)
        {
            for (var i = 0; i < racers.Count; i++)
            {
                if (racers[i].RacerType == RacerType.Player)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("Validated race configuration is missing a player racer.");
        }

        private static int CompareInitialOrder(RacerDefinition left, RacerDefinition right)
        {
            return left.InitialOrder.CompareTo(right.InitialOrder);
        }

        private static int CompareRewardRank(RewardTierDefinition left, RewardTierDefinition right)
        {
            return left.Rank.CompareTo(right.Rank);
        }
    }
}
