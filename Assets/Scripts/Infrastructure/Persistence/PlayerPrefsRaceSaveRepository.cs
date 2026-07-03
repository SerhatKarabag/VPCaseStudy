using System;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;
using UnityEngine;

namespace ThreadRace.Infrastructure.Persistence
{
    public sealed class PlayerPrefsRaceSaveRepository : IRaceSaveRepository
    {
        public RaceSaveLoadResult Load(string saveKey)
        {
            ValidateSaveKey(saveKey);

            if (!PlayerPrefs.HasKey(saveKey))
            {
                return RaceSaveLoadResult.NotFound();
            }

            var json = PlayerPrefs.GetString(saveKey, null);
            if (string.IsNullOrWhiteSpace(json))
            {
                return RaceSaveLoadResult.Failed("Saved race data is empty.");
            }

            try
            {
                var dto = JsonUtility.FromJson<RaceSaveDataDto>(json);
                if (dto == null)
                {
                    return RaceSaveLoadResult.Failed("Saved race data could not be parsed.");
                }

                return RaceSaveLoadResult.Loaded(ToDomain(dto));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                return RaceSaveLoadResult.Failed($"Saved race data is malformed: {exception.Message}");
            }
        }

        public void Save(string saveKey, RaceSaveData saveData)
        {
            ValidateSaveKey(saveKey);

            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            var json = JsonUtility.ToJson(FromDomain(saveData));
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("Race save serialization produced an empty payload.");
            }

            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
        }

        public void Clear(string saveKey)
        {
            ValidateSaveKey(saveKey);

            if (PlayerPrefs.HasKey(saveKey))
            {
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.Save();
            }
        }

        private static RaceSaveData ToDomain(RaceSaveDataDto dto)
        {
            if (dto.racers == null)
            {
                throw new InvalidOperationException("Saved racer collection is missing.");
            }

            if (dto.finishOrder == null)
            {
                throw new InvalidOperationException("Saved finish order collection is missing.");
            }

            var racers = new RaceSaveRacerData[dto.racers.Length];
            for (var i = 0; i < dto.racers.Length; i++)
            {
                var racer = dto.racers[i];
                if (racer == null)
                {
                    throw new InvalidOperationException("Saved racer collection contains a null entry.");
                }

                racers[i] = new RaceSaveRacerData(
                    new RacerId(racer.racerId),
                    racer.progress,
                    racer.isFinished,
                    racer.hasFinishPlacement ? racer.finishPlacement : (int?)null,
                    racer.hasAiStepTimeRemaining ? racer.aiStepTimeRemaining : (float?)null);
            }

            var finishOrder = new RacerId[dto.finishOrder.Length];
            for (var i = 0; i < dto.finishOrder.Length; i++)
            {
                finishOrder[i] = new RacerId(dto.finishOrder[i]);
            }

            if (dto.hasPlayerOutcome && dto.playerOutcome == null)
            {
                throw new InvalidOperationException("Saved player outcome is missing.");
            }

            var outcome = dto.hasPlayerOutcome
                ? new RaceSavePlayerOutcomeData(
                    new RacerId(dto.playerOutcome.playerId),
                    dto.playerOutcome.didFinish,
                    dto.playerOutcome.isDnf,
                    dto.playerOutcome.hasFinishPlacement ? dto.playerOutcome.finishPlacement : (int?)null,
                    dto.playerOutcome.isRewardEligible,
                    dto.playerOutcome.completionReason)
                : null;

            return new RaceSaveData(
                dto.schemaVersion,
                dto.phase,
                racers,
                finishOrder,
                outcome,
                new DeterministicRandomState(
                    dto.randomSeed,
                    dto.randomState,
                    dto.randomConsumedCount),
                dto.revision,
                new RaceSaveTimingData(
                    dto.timingHasStarted,
                    dto.eventStartUtcUnixSeconds,
                    dto.eventEndUtcUnixSeconds,
                    dto.lastObservedUtcUnixSeconds),
                dto.rewardClaimed);
        }

        private static RaceSaveDataDto FromDomain(RaceSaveData saveData)
        {
            var racers = new RaceSaveRacerDataDto[saveData.Racers.Count];
            for (var i = 0; i < saveData.Racers.Count; i++)
            {
                var racer = saveData.Racers[i];
                racers[i] = new RaceSaveRacerDataDto
                {
                    racerId = racer.RacerId.Value,
                    progress = racer.Progress,
                    isFinished = racer.IsFinished,
                    hasFinishPlacement = racer.FinishPlacement.HasValue,
                    finishPlacement = racer.FinishPlacement.GetValueOrDefault(),
                    hasAiStepTimeRemaining = racer.AiStepTimeRemaining.HasValue,
                    aiStepTimeRemaining = racer.AiStepTimeRemaining.GetValueOrDefault()
                };
            }

            var finishOrder = new string[saveData.FinishOrder.Count];
            for (var i = 0; i < saveData.FinishOrder.Count; i++)
            {
                finishOrder[i] = saveData.FinishOrder[i].Value;
            }

            return new RaceSaveDataDto
            {
                schemaVersion = saveData.SchemaVersion,
                phase = saveData.Phase,
                racers = racers,
                finishOrder = finishOrder,
                hasPlayerOutcome = saveData.PlayerOutcome != null,
                playerOutcome = saveData.PlayerOutcome == null ? null : FromDomain(saveData.PlayerOutcome),
                randomSeed = saveData.RandomState.Seed,
                randomState = saveData.RandomState.State,
                randomConsumedCount = saveData.RandomState.ConsumedCount,
                revision = saveData.Revision,
                rewardClaimed = saveData.RewardClaimed,
                timingHasStarted = saveData.TimingData.HasStarted,
                eventStartUtcUnixSeconds = saveData.TimingData.StartUtcUnixSeconds,
                eventEndUtcUnixSeconds = saveData.TimingData.EndUtcUnixSeconds,
                lastObservedUtcUnixSeconds = saveData.TimingData.LastObservedUtcUnixSeconds
            };
        }

        private static RaceSavePlayerOutcomeDto FromDomain(RaceSavePlayerOutcomeData outcome)
        {
            return new RaceSavePlayerOutcomeDto
            {
                playerId = outcome.PlayerId.Value,
                didFinish = outcome.DidFinish,
                isDnf = outcome.IsDnf,
                hasFinishPlacement = outcome.FinishPlacement.HasValue,
                finishPlacement = outcome.FinishPlacement.GetValueOrDefault(),
                isRewardEligible = outcome.IsRewardEligible,
                completionReason = outcome.CompletionReason
            };
        }

        private static void ValidateSaveKey(string saveKey)
        {
            if (string.IsNullOrWhiteSpace(saveKey))
            {
                throw new ArgumentException("Save key must not be empty.", nameof(saveKey));
            }
        }

        [Serializable]
        private sealed class RaceSaveDataDto
        {
            public int schemaVersion;
            public RacePhase phase;
            public RaceSaveRacerDataDto[] racers;
            public string[] finishOrder;
            public bool hasPlayerOutcome;
            public RaceSavePlayerOutcomeDto playerOutcome;
            public int randomSeed;
            public int randomState;
            public int randomConsumedCount;
            public int revision;
            public bool rewardClaimed;
            public bool timingHasStarted;
            public long eventStartUtcUnixSeconds;
            public long eventEndUtcUnixSeconds;
            public long lastObservedUtcUnixSeconds;
        }

        [Serializable]
        private sealed class RaceSaveRacerDataDto
        {
            public string racerId;
            public int progress;
            public bool isFinished;
            public bool hasFinishPlacement;
            public int finishPlacement;
            public bool hasAiStepTimeRemaining;
            public float aiStepTimeRemaining;
        }

        [Serializable]
        private sealed class RaceSavePlayerOutcomeDto
        {
            public string playerId;
            public bool didFinish;
            public bool isDnf;
            public bool hasFinishPlacement;
            public int finishPlacement;
            public bool isRewardEligible;
            public RaceCompletionReason completionReason;
        }
    }
}
