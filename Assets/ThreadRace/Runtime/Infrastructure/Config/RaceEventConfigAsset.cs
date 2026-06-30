using System;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using UnityEngine;

namespace ThreadRace.Infrastructure.Config
{
    [CreateAssetMenu(fileName = "RaceEventConfigAsset", menuName = "Thread Race/Race Event Config")]
    public sealed class RaceEventConfigAsset : ScriptableObject
    {
        [SerializeField] private int _saveSchemaVersion = 1;
        [SerializeField] private string _saveKey = "ThreadRace.Save.V1";
        [SerializeField] private int _defaultSeed = 26062026;
        [SerializeField] private int _finishTarget = RaceConfiguration.DefaultFinishTarget;
        [SerializeField] private int _rewardedPositionCount = RaceConfiguration.DefaultRewardedPositionCount;
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

            var configuration = new RaceConfiguration(
                definitions,
                _finishTarget,
                _rewardedPositionCount);

            return new RaceEventSettings(
                configuration,
                _saveSchemaVersion,
                _saveKey,
                _defaultSeed);
        }

        public void Configure(
            int saveSchemaVersion,
            string saveKey,
            int defaultSeed,
            int finishTarget,
            int rewardedPositionCount,
            RacerAuthoringData[] racers)
        {
            _saveSchemaVersion = saveSchemaVersion;
            _saveKey = saveKey;
            _defaultSeed = defaultSeed;
            _finishTarget = finishTarget;
            _rewardedPositionCount = rewardedPositionCount;

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

        [Serializable]
        public sealed class RacerAuthoringData
        {
            [SerializeField] private string _racerId;
            [SerializeField] private string _displayName;
            [SerializeField] private RacerType _racerType;
            [SerializeField] private float _minimumAiStepDelaySeconds = 1f;
            [SerializeField] private float _maximumAiStepDelaySeconds = 1f;

            public RacerAuthoringData(
                string racerId,
                string displayName,
                RacerType racerType,
                float minimumAiStepDelaySeconds,
                float maximumAiStepDelaySeconds)
            {
                _racerId = racerId;
                _displayName = displayName;
                _racerType = racerType;
                _minimumAiStepDelaySeconds = minimumAiStepDelaySeconds;
                _maximumAiStepDelaySeconds = maximumAiStepDelaySeconds;
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
                        _maximumAiStepDelaySeconds);
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
                    _maximumAiStepDelaySeconds);
            }
        }
    }
}
