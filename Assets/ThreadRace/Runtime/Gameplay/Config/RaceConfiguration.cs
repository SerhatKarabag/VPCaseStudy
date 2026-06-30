using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Config
{
    public sealed class RaceConfiguration
    {
        public const int RequiredRacerCount = 5;
        public const int RequiredPlayerCount = 1;
        public const int RequiredAiCount = 4;
        public const int DefaultFinishTarget = 10;
        public const int DefaultRewardedPositionCount = 3;

        private readonly RacerDefinition[] _racers;
        private readonly ReadOnlyCollection<RacerDefinition> _racersView;

        public RaceConfiguration(
            IEnumerable<RacerDefinition> racers,
            int finishTarget,
            int rewardedPositionCount)
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

            if (racerList.Count != RequiredRacerCount)
            {
                throw new ArgumentException($"Race configuration must contain exactly {RequiredRacerCount} racers.", nameof(racers));
            }

            if (rewardedPositionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardedPositionCount), "Rewarded position count must be greater than zero.");
            }

            if (rewardedPositionCount > racerList.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardedPositionCount), "Rewarded position count must not exceed racer count.");
            }

            ValidateRacers(racerList);

            _racers = racerList.ToArray();
            Array.Sort(_racers, CompareInitialOrder);
            _racersView = Array.AsReadOnly(_racers);
            FinishTarget = finishTarget;
            RewardedPositionCount = rewardedPositionCount;
            PlayerRacerIndex = FindPlayerRacerIndex(_racers);
        }

        public int FinishTarget { get; }

        public int RewardedPositionCount { get; }

        public int PlayerRacerIndex { get; }

        public IReadOnlyList<RacerDefinition> Racers => _racersView;

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

        private static List<RacerDefinition> CopyRacers(IEnumerable<RacerDefinition> racers)
        {
            var copied = new List<RacerDefinition>(RequiredRacerCount);

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

            if (aiCount != RequiredAiCount)
            {
                throw new ArgumentException($"Race configuration must contain exactly {RequiredAiCount} AI racers.", nameof(racers));
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
    }
}
