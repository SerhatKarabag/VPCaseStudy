using System;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;

namespace ThreadRace.Gameplay.Application
{
    public sealed class RaceSession
    {
        private const float TimerEpsilon = 0.00001f;

        private readonly RaceConfiguration _configuration;
        private readonly IDeterministicRandomSource _randomSource;
        private readonly RacerRuntimeState[] _racers;
        private readonly AiRacePlan[] _aiPlans;
        private readonly int[] _rankingOrder;
        private readonly RaceFinishTracker _finishTracker;
        private readonly int _playerIndex;

        private RacePhase _phase;
        private PlayerRaceOutcome _finalOutcome;
        private bool _rewardClaimed;

        public RaceSession(RaceConfiguration configuration, IDeterministicRandomSource randomSource)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            _racers = new RacerRuntimeState[_configuration.Racers.Count];
            _aiPlans = new AiRacePlan[_configuration.Racers.Count];
            _rankingOrder = new int[_configuration.Racers.Count];
            _finishTracker = new RaceFinishTracker(_configuration.Racers.Count);
            _playerIndex = _configuration.PlayerRacerIndex;

            for (var i = 0; i < _racers.Length; i++)
            {
                _racers[i] = new RacerRuntimeState(_configuration.GetRacer(i));
                _rankingOrder[i] = i;

                if (_configuration.GetRacer(i).RacerType == RacerType.Ai)
                {
                    _aiPlans[i] = AiRacePlanGenerator.Generate(
                        _configuration.GetRacer(i),
                        _configuration.FinishTarget,
                        _randomSource.Seed);
                }
            }

            RaceRankingService.Recalculate(_racers, _rankingOrder);
        }

        public RacePhase Phase => _phase;

        public bool RewardClaimed => _rewardClaimed;

        public int Revision { get; private set; }

        public DeterministicRandomState RandomState => _randomSource.CurrentState;

        public bool Start()
        {
            if (_phase != RacePhase.NotStarted)
            {
                throw new InvalidOperationException("Race can only be started once.");
            }

            _phase = RacePhase.Running;

            for (var i = 0; i < _racers.Length; i++)
            {
                var racer = _racers[i];
                if (racer.Definition.RacerType == RacerType.Ai)
                {
                    racer.AiStepTimeRemaining = GetNextAiDelay(i);
                }
            }

            Revision++;
            return true;
        }

        public bool ApplyPlayerResult(LevelResult result)
        {
            EnsureRunning("Player level results can only be applied while the race is running.");
            ValidateLevelResult(result);

            if (result == LevelResult.Fail)
            {
                return false;
            }

            return AdvanceRacer(_playerIndex);
        }

        public bool AdvanceAi(float deltaTimeSeconds)
        {
            EnsureRunning("AI simulation can only advance while the race is running.");
            ValidateDeltaTime(deltaTimeSeconds);

            if (deltaTimeSeconds == 0f || !HasActiveAiRacers())
            {
                return false;
            }

            var revisionBefore = Revision;
            var remainingDelta = deltaTimeSeconds;
            while (_phase == RacePhase.Running && remainingDelta > 0f && TryGetNextAiTimer(out var nextTimer))
            {
                if (nextTimer - remainingDelta > TimerEpsilon)
                {
                    SubtractActiveAiTimers(remainingDelta);
                    return Revision != revisionBefore;
                }

                SubtractActiveAiTimers(nextTimer);
                remainingDelta -= nextTimer;
                if (remainingDelta < TimerEpsilon)
                {
                    remainingDelta = 0f;
                }

                ProcessDueAiRacers();
            }

            return Revision != revisionBefore;
        }

        public RaceSnapshot GetSnapshot()
        {
            return RaceSnapshotFactory.Create(
                _configuration,
                _phase,
                _racers,
                _rankingOrder,
                _finishTracker,
                _finalOutcome,
                _rewardClaimed);
        }

        public PlayerRaceOutcome GetFinalOutcome()
        {
            if ((_phase != RacePhase.Reward && _phase != RacePhase.Completed) || _finalOutcome == null)
            {
                throw new InvalidOperationException("Final outcome is only available after the race outcome is resolved.");
            }

            return _finalOutcome;
        }

        public RaceSaveData CaptureSaveData(int schemaVersion, RaceEventTimingState timingState = null)
        {
            return RaceSaveSnapshotFactory.Capture(
                schemaVersion,
                _phase,
                _racers,
                _finishTracker,
                _finalOutcome,
                _rewardClaimed,
                _randomSource.CurrentState,
                Revision,
                timingState);
        }

        public bool ClaimReward()
        {
            if (_phase == RacePhase.Completed)
            {
                return false;
            }

            if (_phase != RacePhase.Reward || _finalOutcome == null)
            {
                throw new InvalidOperationException("Reward can only be claimed after the race outcome is resolved.");
            }

            _rewardClaimed = _finalOutcome.IsRewardEligible;
            _phase = RacePhase.Completed;
            Revision++;
            return true;
        }

        internal static RaceSession Restore(
            RaceConfiguration configuration,
            IDeterministicRandomSource randomSource,
            RaceSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            var session = new RaceSession(configuration, randomSource);
            session.RestoreFromSave(saveData);
            return session;
        }

        private void RestoreFromSave(RaceSaveData saveData)
        {
            _phase = saveData.Phase;

            for (var i = 0; i < saveData.Racers.Count; i++)
            {
                var racerData = saveData.Racers[i];
                var racerIndex = _configuration.GetRacerIndex(racerData.RacerId);
                if (racerIndex < 0)
                {
                    throw new InvalidOperationException($"Cannot restore unknown racer '{racerData.RacerId}'.");
                }

                _racers[racerIndex].Restore(
                    racerData.Progress,
                    racerData.IsFinished,
                    racerData.FinishPlacement,
                    racerData.AiStepTimeRemaining);
            }

            _finishTracker.Restore(_configuration, saveData);
            RaceRankingService.Recalculate(_racers, _rankingOrder);
            _finalOutcome = saveData.PlayerOutcome == null ? null : CreatePlayerOutcome(saveData.PlayerOutcome);
            _rewardClaimed = saveData.RewardClaimed;
            Revision = saveData.Revision;
        }

        private bool AdvanceRacer(int racerIndex)
        {
            var racer = _racers[racerIndex];
            var progressed = racer.AdvanceOneStep(_configuration.FinishTarget);
            if (!progressed)
            {
                return false;
            }

            if (racer.Progress == _configuration.FinishTarget && !racer.IsFinished)
            {
                _finishTracker.RecordFinish(racerIndex, _racers);
            }

            RaceRankingService.Recalculate(_racers, _rankingOrder);
            ResolveOutcomeIfNeeded();
            Revision++;
            return true;
        }

        private void ResolveOutcomeIfNeeded()
        {
            var outcome = RaceOutcomeResolver.ResolveIfNeeded(
                _phase,
                _racers,
                _playerIndex,
                _configuration.RewardedPositionCount,
                _finishTracker);
            if (outcome != null)
            {
                CompleteRace(outcome);
            }
        }

        public bool ExpireEvent()
        {
            if (_phase == RacePhase.Reward || _phase == RacePhase.Completed)
            {
                return false;
            }

            if (_phase != RacePhase.NotStarted && _phase != RacePhase.Running)
            {
                throw new InvalidOperationException("The race can only expire before completion.");
            }

            var player = _racers[_playerIndex];
            if (player.IsFinished)
            {
                return false;
            }

            CompleteRace(RaceOutcomeResolver.CreateExpiredOutcome(_racers, _playerIndex, _finishTracker));
            RaceRankingService.Recalculate(_racers, _rankingOrder);
            Revision++;
            return true;
        }

        private void CompleteRace(PlayerRaceOutcome outcome)
        {
            _finalOutcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _rewardClaimed = false;
            _phase = RacePhase.Reward;
        }

        private bool TryGetNextAiTimer(out float nextTimer)
        {
            nextTimer = float.MaxValue;
            var found = false;

            for (var i = 0; i < _racers.Length; i++)
            {
                var racer = _racers[i];
                if (!IsActiveAi(racer))
                {
                    continue;
                }

                var timer = racer.AiStepTimeRemaining;
                if (timer < 0f)
                {
                    throw new InvalidOperationException($"AI racer '{racer.Definition.Id}' has a negative step timer.");
                }

                if (timer <= TimerEpsilon)
                {
                    nextTimer = 0f;
                    found = true;
                    continue;
                }

                if (timer < nextTimer)
                {
                    nextTimer = timer;
                    found = true;
                }
            }

            return found;
        }

        private void SubtractActiveAiTimers(float elapsed)
        {
            for (var i = 0; i < _racers.Length; i++)
            {
                var racer = _racers[i];
                if (!IsActiveAi(racer))
                {
                    continue;
                }

                var remaining = racer.AiStepTimeRemaining - elapsed;
                racer.AiStepTimeRemaining = remaining <= TimerEpsilon ? 0f : remaining;
            }
        }

        private void ProcessDueAiRacers()
        {
            for (var i = 0; i < _racers.Length && _phase == RacePhase.Running; i++)
            {
                var racer = _racers[i];
                if (!IsActiveAi(racer) || racer.AiStepTimeRemaining > TimerEpsilon)
                {
                    continue;
                }

                AdvanceRacer(i);

                if (_phase == RacePhase.Running && !racer.IsFinished)
                {
                    racer.AiStepTimeRemaining = GetNextAiDelay(i);
                }
            }
        }

        private bool HasActiveAiRacers()
        {
            for (var i = 0; i < _racers.Length; i++)
            {
                if (IsActiveAi(_racers[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsActiveAi(RacerRuntimeState racer)
        {
            return racer.Definition.RacerType == RacerType.Ai && !racer.IsFinished;
        }

        private float GetNextAiDelay(int racerIndex)
        {
            var racer = _racers[racerIndex];
            var plan = _aiPlans[racerIndex];
            if (plan == null)
            {
                throw new InvalidOperationException($"AI racer '{racer.Definition.Id}' is missing a race plan.");
            }

            var delay = plan.GetDelayForNextStep(racer.Progress);
            if (float.IsNaN(delay) || float.IsInfinity(delay) || delay <= 0f)
            {
                throw new InvalidOperationException($"AI racer '{racer.Definition.Id}' generated an invalid step delay.");
            }

            return delay;
        }

        private PlayerRaceOutcome CreatePlayerOutcome(RaceSavePlayerOutcomeData outcomeData)
        {
            return new PlayerRaceOutcome(
                outcomeData.PlayerId,
                outcomeData.DidFinish,
                outcomeData.IsDnf,
                outcomeData.FinishPlacement,
                outcomeData.IsRewardEligible,
                outcomeData.CompletionReason,
                _finishTracker.CreateFinishedResults(_racers));
        }

        private void EnsureRunning(string message)
        {
            if (_phase != RacePhase.Running)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void ValidateDeltaTime(float deltaTimeSeconds)
        {
            if (float.IsNaN(deltaTimeSeconds) || float.IsInfinity(deltaTimeSeconds))
            {
                throw new ArgumentException("Delta time must be finite.", nameof(deltaTimeSeconds));
            }

            if (deltaTimeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds), "Delta time must not be negative.");
            }
        }

        private static void ValidateLevelResult(LevelResult result)
        {
            if (result != LevelResult.Success && result != LevelResult.Fail)
            {
                throw new ArgumentOutOfRangeException(nameof(result), "Unsupported level result.");
            }
        }
    }
}
