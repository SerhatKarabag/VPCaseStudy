using System;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using UnityEngine;

namespace ThreadRace.Infrastructure.Config
{
    [CreateAssetMenu(fileName = "RaceEventConfigAsset", menuName = "Thread Race/Race Event Config")]
    public sealed class RaceEventConfigAsset : ScriptableObject
    {
        [SerializeField] private int _saveSchemaVersion = RaceEventSettings.CurrentSaveSchemaVersion;
        [SerializeField] private string _saveKey = RaceEventSettings.CurrentSaveKey;
        [SerializeField] private int _defaultSeed = 26062026;
        [SerializeField] private long _eventDurationSeconds = RaceEventSettings.DefaultEventDurationSeconds;
        [SerializeField] private int _countdownUpdateIntervalSeconds = RaceEventSettings.DefaultCountdownUpdateIntervalSeconds;
        [SerializeField] private int _finishTarget = RaceConfiguration.DefaultFinishTarget;
        [SerializeField] private int _rewardedPositionCount = RaceConfiguration.DefaultRewardedPositionCount;
        [SerializeField] private RewardTierAuthoringData[] _rewardTiers = Array.Empty<RewardTierAuthoringData>();
        [SerializeField] private RacerAuthoringData[] _racers = Array.Empty<RacerAuthoringData>();

        public RaceEventSettings ToRuntimeSettings()
        {
            var definitions = new RacerDefinition[_racers == null ? 0 : _racers.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                if (_racers[i] == null)
                {
                    throw new InvalidOperationException($"Race config contains a null racer entry at index {i}.");
                }

                definitions[i] = _racers[i].ToDefinition(i);
            }

            var rewardTiers = CreateRewardTierDefinitions();
            if (_rewardedPositionCount != rewardTiers.Length)
            {
                throw new InvalidOperationException(
                    $"Rewarded position count '{_rewardedPositionCount}' must match authored reward tier count '{rewardTiers.Length}'.");
            }

            var configuration = new RaceConfiguration(
                definitions,
                _finishTarget,
                rewardTiers);

            return new RaceEventSettings(
                configuration,
                _saveSchemaVersion,
                _saveKey,
                _defaultSeed,
                _eventDurationSeconds,
                _countdownUpdateIntervalSeconds);
        }

        public void Configure(
            int saveSchemaVersion,
            string saveKey,
            int defaultSeed,
            long eventDurationSeconds,
            int countdownUpdateIntervalSeconds,
            int finishTarget,
            int rewardedPositionCount,
            RacerAuthoringData[] racers)
        {
            Configure(
                saveSchemaVersion,
                saveKey,
                defaultSeed,
                eventDurationSeconds,
                countdownUpdateIntervalSeconds,
                finishTarget,
                rewardedPositionCount,
                null,
                racers);
        }

        public void Configure(
            int saveSchemaVersion,
            string saveKey,
            int defaultSeed,
            long eventDurationSeconds,
            int countdownUpdateIntervalSeconds,
            int finishTarget,
            int rewardedPositionCount,
            RewardTierAuthoringData[] rewardTiers,
            RacerAuthoringData[] racers)
        {
            _saveSchemaVersion = saveSchemaVersion;
            _saveKey = saveKey;
            _defaultSeed = defaultSeed;
            _eventDurationSeconds = eventDurationSeconds;
            _countdownUpdateIntervalSeconds = countdownUpdateIntervalSeconds;
            _finishTarget = finishTarget;
            _rewardedPositionCount = rewardedPositionCount;
            _rewardTiers = CloneRewardTiers(rewardTiers);

            if (racers == null)
            {
                _racers = null;
                return;
            }

            _racers = new RacerAuthoringData[racers.Length];
            for (var i = 0; i < racers.Length; i++)
            {
                _racers[i] = racers[i] == null ? null : racers[i].Clone();
            }
        }

        private RewardTierDefinition[] CreateRewardTierDefinitions()
        {
            var source = _rewardTiers;
            if (source == null || source.Length == 0)
            {
                throw new InvalidOperationException("Race config requires at least one authored reward tier.");
            }

            var rewardTiers = new RewardTierDefinition[source.Length];
            for (var i = 0; i < rewardTiers.Length; i++)
            {
                if (source[i] == null)
                {
                    throw new InvalidOperationException($"Race config contains a null reward tier entry at index {i}.");
                }

                rewardTiers[i] = source[i].ToDefinition();
            }

            return rewardTiers;
        }

        private static RewardTierAuthoringData[] CloneRewardTiers(RewardTierAuthoringData[] rewardTiers)
        {
            if (rewardTiers == null)
            {
                return null;
            }

            var cloned = new RewardTierAuthoringData[rewardTiers.Length];
            for (var i = 0; i < rewardTiers.Length; i++)
            {
                cloned[i] = rewardTiers[i] == null ? null : rewardTiers[i].Clone();
            }

            return cloned;
        }

        [Serializable]
        public sealed class RewardTierAuthoringData
        {
            [SerializeField] private int _rank;
            [SerializeField] private string _rewardId;
            [SerializeField] private RewardType _rewardType;
            [SerializeField] private int _amount;
            [SerializeField] private string _displayText;
            [SerializeField] private string _iconId;

            public RewardTierAuthoringData(
                int rank,
                string rewardId,
                RewardType rewardType,
                int amount,
                string displayText,
                string iconId)
            {
                _rank = rank;
                _rewardId = rewardId;
                _rewardType = rewardType;
                _amount = amount;
                _displayText = displayText;
                _iconId = iconId;
            }

            public RewardTierAuthoringData(RewardTierDefinition definition)
                : this(
                    definition.Rank,
                    definition.RewardId,
                    definition.RewardType,
                    definition.Amount,
                    definition.DisplayText,
                    definition.IconId)
            {
            }

            public RewardTierDefinition ToDefinition()
            {
                if (string.IsNullOrWhiteSpace(_iconId))
                {
                    throw new InvalidOperationException(
                        $"Reward tier rank '{_rank}' requires an icon ID. Presentation sprites must be bound separately by ID.");
                }

                return new RewardTierDefinition(
                    _rank,
                    _rewardId,
                    _rewardType,
                    _amount,
                    _displayText,
                    _iconId);
            }

            public RewardTierAuthoringData Clone()
            {
                return new RewardTierAuthoringData(
                    _rank,
                    _rewardId,
                    _rewardType,
                    _amount,
                    _displayText,
                    _iconId);
            }
        }

        [Serializable]
        public sealed class RacerAuthoringData
        {
            [SerializeField] private string _racerId;
            [SerializeField] private string _displayName;
            [SerializeField] private RacerType _racerType;
            [SerializeField] private float _minimumAiStepDelaySeconds = 1f;
            [SerializeField] private float _maximumAiStepDelaySeconds = 1f;
            [SerializeField] private AiPacingStyle _aiPacingStyle = AiPacingStyle.LegacyFixed;
            [SerializeField] private bool _usesDynamicAiPlanning;
            [SerializeField, Range(0f, 1f)] private float _aiSkill = 0.5f;
            [SerializeField, Range(0f, 1f)] private float _aiConsistency = 1f;
            [SerializeField, Range(0f, 1f)] private float _aiVolatility;
            [SerializeField, Range(-1f, 1f)] private float _aiEarlyPaceBias;
            [SerializeField, Range(-1f, 1f)] private float _aiLatePaceBias;
            [SerializeField, Range(0f, 1f)] private float _aiBurstChance;
            [SerializeField, Range(0f, 1f)] private float _aiSlumpChance;
            [SerializeField, Range(0f, 1f)] private float _aiFinalPushChance;

            public RacerAuthoringData(
                string racerId,
                string displayName,
                RacerType racerType,
                float minimumAiStepDelaySeconds,
                float maximumAiStepDelaySeconds,
                AiPacingStyle aiPacingStyle = AiPacingStyle.LegacyFixed)
                : this(
                    racerId,
                    displayName,
                    racerType,
                    minimumAiStepDelaySeconds,
                    maximumAiStepDelaySeconds,
                    aiPacingStyle,
                    aiPacingStyle != AiPacingStyle.LegacyFixed,
                    0.5f,
                    0.75f,
                    0.25f,
                    0f,
                    0f,
                    0.08f,
                    0.06f,
                    0.08f)
            {
            }

            public RacerAuthoringData(
                string racerId,
                string displayName,
                RacerType racerType,
                float minimumAiStepDelaySeconds,
                float maximumAiStepDelaySeconds,
                AiPacingStyle aiPacingStyle,
                bool usesDynamicAiPlanning,
                float aiSkill,
                float aiConsistency,
                float aiVolatility,
                float aiEarlyPaceBias,
                float aiLatePaceBias,
                float aiBurstChance,
                float aiSlumpChance,
                float aiFinalPushChance)
            {
                _racerId = racerId;
                _displayName = displayName;
                _racerType = racerType;
                _minimumAiStepDelaySeconds = minimumAiStepDelaySeconds;
                _maximumAiStepDelaySeconds = maximumAiStepDelaySeconds;
                _aiPacingStyle = aiPacingStyle;
                _usesDynamicAiPlanning = usesDynamicAiPlanning;
                _aiSkill = aiSkill;
                _aiConsistency = aiConsistency;
                _aiVolatility = aiVolatility;
                _aiEarlyPaceBias = aiEarlyPaceBias;
                _aiLatePaceBias = aiLatePaceBias;
                _aiBurstChance = aiBurstChance;
                _aiSlumpChance = aiSlumpChance;
                _aiFinalPushChance = aiFinalPushChance;
            }

            public RacerDefinition ToDefinition(int initialOrder)
            {
                if (_racerType == RacerType.Player)
                {
                    return RacerDefinition.CreatePlayer(_racerId, _displayName, initialOrder);
                }

                if (_racerType == RacerType.Ai)
                {
                    return RacerDefinition.CreateAi(
                        _racerId,
                        _displayName,
                        initialOrder,
                        _minimumAiStepDelaySeconds,
                        _maximumAiStepDelaySeconds,
                        new AiPacingProfile(
                            _aiPacingStyle,
                            _usesDynamicAiPlanning,
                            _aiSkill,
                            _aiConsistency,
                            _aiVolatility,
                            _aiEarlyPaceBias,
                            _aiLatePaceBias,
                            _aiBurstChance,
                            _aiSlumpChance,
                            _aiFinalPushChance));
                }

                throw new InvalidOperationException($"Racer '{_racerId}' has unsupported racer type '{_racerType}'.");
            }

            public RacerAuthoringData Clone()
            {
                return new RacerAuthoringData(
                    _racerId,
                    _displayName,
                    _racerType,
                    _minimumAiStepDelaySeconds,
                    _maximumAiStepDelaySeconds,
                    _aiPacingStyle,
                    _usesDynamicAiPlanning,
                    _aiSkill,
                    _aiConsistency,
                    _aiVolatility,
                    _aiEarlyPaceBias,
                    _aiLatePaceBias,
                    _aiBurstChance,
                    _aiSlumpChance,
                    _aiFinalPushChance);
            }
        }
    }
}
