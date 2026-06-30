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
        private readonly int[] _rankingOrder;
        private readonly int[] _finishOrder;
        private readonly int _playerIndex;

        private RacePhase _phase;
        private int _finishCount;
        private PlayerRaceOutcome _finalOutcome;

        public RaceSession(RaceConfiguration configuration, IDeterministicRandomSource randomSource)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            _racers = new RacerRuntimeState[_configuration.Racers.Count];
            _rankingOrder = new int[_configuration.Racers.Count];
            _finishOrder = new int[_configuration.Racers.Count];
            _playerIndex = _configuration.PlayerRacerIndex;

            for (var i = 0; i < _racers.Length; i++)
            {
                _racers[i] = new RacerRuntimeState(_configuration.GetRacer(i));
                _rankingOrder[i] = i;
                _finishOrder[i] = -1;
            }

            RecalculateRanking();
        }

        public RacePhase Phase => _phase;

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
                    racer.AiStepTimeRemaining = GenerateAiDelay(racer.Definition);
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
                if (nextTimer > remainingDelta)
                {
                    SubtractActiveAiTimers(remainingDelta);
                    return Revision != revisionBefore;
                }

                SubtractActiveAiTimers(nextTimer);
                remainingDelta -= nextTimer;
                ProcessDueAiRacers();
            }

            return Revision != revisionBefore;
        }

        public RaceSnapshot GetSnapshot()
        {
            return CreateSnapshot();
        }

        public PlayerRaceOutcome GetFinalOutcome()
        {
            if (_phase != RacePhase.Completed || _finalOutcome == null)
            {
                throw new InvalidOperationException("Final outcome is only available after the race is completed.");
            }

            return _finalOutcome;
        }

        public RaceSaveData CaptureSaveData(int schemaVersion)
        {
            var racers = new RaceSaveRacerData[_racers.Length];
            for (var i = 0; i < _racers.Length; i++)
            {
                var racer = _racers[i];
                var aiStepTimeRemaining = racer.Definition.RacerType == RacerType.Ai
                    && _phase == RacePhase.Running
                    && !racer.IsFinished
                        ? racer.AiStepTimeRemaining
                        : (float?)null;

                racers[i] = new RaceSaveRacerData(
                    racer.Definition.Id,
                    racer.Progress,
                    racer.IsFinished,
                    racer.HasFinishPlacement ? racer.FinishPlacement : (int?)null,
                    aiStepTimeRemaining);
            }

            var finishOrder = new RacerId[_finishCount];
            for (var i = 0; i < _finishCount; i++)
            {
                finishOrder[i] = _racers[_finishOrder[i]].Definition.Id;
            }

            return new RaceSaveData(
                schemaVersion,
                _phase,
                racers,
                finishOrder,
                _finalOutcome == null ? null : CreateSaveOutcome(_finalOutcome),
                _randomSource.CurrentState,
                Revision);
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
            _finishCount = saveData.FinishOrder.Count;

            for (var i = 0; i < _finishOrder.Length; i++)
            {
                _finishOrder[i] = -1;
            }

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

            for (var i = 0; i < saveData.FinishOrder.Count; i++)
            {
                var racerIndex = _configuration.GetRacerIndex(saveData.FinishOrder[i]);
                if (racerIndex < 0)
                {
                    throw new InvalidOperationException($"Cannot restore unknown finisher '{saveData.FinishOrder[i]}'.");
                }

                _finishOrder[i] = racerIndex;
            }

            RecalculateRanking();
            _finalOutcome = saveData.PlayerOutcome == null ? null : CreatePlayerOutcome(saveData.PlayerOutcome);
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
                RecordFinish(racerIndex);
            }

            RecalculateRanking();
            ResolveOutcomeIfNeeded();
            Revision++;
            return true;
        }

        private void RecordFinish(int racerIndex)
        {
            var racer = _racers[racerIndex];
            _finishOrder[_finishCount] = racerIndex;
            _finishCount++;
            racer.RecordFinished(_finishCount);
        }

        private void ResolveOutcomeIfNeeded()
        {
            if (_phase != RacePhase.Running)
            {
                return;
            }

            var player = _racers[_playerIndex];
            if (player.IsFinished)
            {
                CompleteRace(new PlayerRaceOutcome(
                    player.Definition.Id,
                    true,
                    false,
                    player.FinishPlacement,
                    player.FinishPlacement <= _configuration.RewardedPositionCount,
                    CreateFinishedResults()));
                return;
            }

            if (_finishCount >= _configuration.RewardedPositionCount)
            {
                CompleteRace(new PlayerRaceOutcome(
                    player.Definition.Id,
                    false,
                    true,
                    null,
                    false,
                    CreateFinishedResults()));
            }
        }

        private void CompleteRace(PlayerRaceOutcome outcome)
        {
            _finalOutcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _phase = RacePhase.Completed;
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
                if (timer <= 0f)
                {
                    throw new InvalidOperationException($"AI racer '{racer.Definition.Id}' has a non-positive step timer.");
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
                    racer.AiStepTimeRemaining = GenerateAiDelay(racer.Definition);
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

        private float GenerateAiDelay(RacerDefinition racerDefinition)
        {
            if (racerDefinition.AiStepTiming == null)
            {
                throw new InvalidOperationException($"AI racer '{racerDefinition.Id}' is missing step timing.");
            }

            var delay = _randomSource.Range(
                racerDefinition.AiStepTiming.MinimumStepDelaySeconds,
                racerDefinition.AiStepTiming.MaximumStepDelaySeconds);

            if (float.IsNaN(delay) || float.IsInfinity(delay) || delay <= 0f)
            {
                throw new InvalidOperationException($"AI racer '{racerDefinition.Id}' generated an invalid step delay.");
            }

            return delay;
        }

        private void RecalculateRanking()
        {
            for (var i = 0; i < _rankingOrder.Length; i++)
            {
                _rankingOrder[i] = i;
            }

            for (var i = 1; i < _rankingOrder.Length; i++)
            {
                var candidate = _rankingOrder[i];
                var previousIndex = i - 1;

                while (previousIndex >= 0 && CompareRacers(candidate, _rankingOrder[previousIndex]) < 0)
                {
                    _rankingOrder[previousIndex + 1] = _rankingOrder[previousIndex];
                    previousIndex--;
                }

                _rankingOrder[previousIndex + 1] = candidate;
            }

            for (var rankIndex = 0; rankIndex < _rankingOrder.Length; rankIndex++)
            {
                _racers[_rankingOrder[rankIndex]].CurrentRank = rankIndex + 1;
            }
        }

        private int CompareRacers(int leftIndex, int rightIndex)
        {
            var left = _racers[leftIndex];
            var right = _racers[rightIndex];

            if (left.IsFinished && right.IsFinished)
            {
                return left.FinishPlacement.CompareTo(right.FinishPlacement);
            }

            if (left.IsFinished)
            {
                return -1;
            }

            if (right.IsFinished)
            {
                return 1;
            }

            var progressComparison = right.Progress.CompareTo(left.Progress);
            if (progressComparison != 0)
            {
                return progressComparison;
            }

            return left.Definition.InitialOrder.CompareTo(right.Definition.InitialOrder);
        }

        private RaceSnapshot CreateSnapshot()
        {
            var racerSnapshots = new RacerSnapshot[_racers.Length];
            for (var i = 0; i < _racers.Length; i++)
            {
                var racer = _racers[i];
                racerSnapshots[i] = new RacerSnapshot(
                    racer.Definition.Id,
                    racer.Definition.DisplayName,
                    racer.Definition.RacerType,
                    racer.Progress,
                    racer.CurrentRank,
                    racer.IsFinished,
                    racer.HasFinishPlacement ? racer.FinishPlacement : (int?)null);
            }

            var rankingEntries = new RaceRankingEntry[_rankingOrder.Length];
            for (var i = 0; i < _rankingOrder.Length; i++)
            {
                var racer = _racers[_rankingOrder[i]];
                rankingEntries[i] = new RaceRankingEntry(
                    racer.Definition.Id,
                    racer.CurrentRank,
                    racer.Progress,
                    racer.IsFinished,
                    racer.HasFinishPlacement ? racer.FinishPlacement : (int?)null);
            }

            return new RaceSnapshot(
                _phase,
                _configuration.FinishTarget,
                _configuration.RewardedPositionCount,
                racerSnapshots,
                rankingEntries,
                CreateFinishedResults(),
                _finalOutcome);
        }

        private FinishedRacerResult[] CreateFinishedResults()
        {
            var finishers = new FinishedRacerResult[_finishCount];
            for (var i = 0; i < _finishCount; i++)
            {
                var racer = _racers[_finishOrder[i]];
                finishers[i] = new FinishedRacerResult(racer.Definition.Id, racer.FinishPlacement);
            }

            return finishers;
        }

        private PlayerRaceOutcome CreatePlayerOutcome(RaceSavePlayerOutcomeData outcomeData)
        {
            return new PlayerRaceOutcome(
                outcomeData.PlayerId,
                outcomeData.DidFinish,
                outcomeData.IsDnf,
                outcomeData.FinishPlacement,
                outcomeData.IsRewardEligible,
                CreateFinishedResults());
        }

        private static RaceSavePlayerOutcomeData CreateSaveOutcome(PlayerRaceOutcome outcome)
        {
            return new RaceSavePlayerOutcomeData(
                outcome.PlayerId,
                outcome.DidFinish,
                outcome.IsDnf,
                outcome.FinishPlacement,
                outcome.IsRewardEligible);
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
