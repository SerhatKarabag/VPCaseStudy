using System;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Gameplay.Persistence;

namespace ThreadRace.Gameplay.Application
{
    internal sealed class RaceFinishTracker
    {
        private readonly int[] _finishOrder;

        public RaceFinishTracker(int racerCount)
        {
            if (racerCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(racerCount), "Racer count must be greater than zero.");
            }

            _finishOrder = new int[racerCount];
            Reset();
        }

        public int FinishCount { get; private set; }

        public void RecordFinish(int racerIndex, RacerRuntimeState[] racers)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            if (racerIndex < 0 || racerIndex >= racers.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(racerIndex));
            }

            if (FinishCount >= _finishOrder.Length)
            {
                throw new InvalidOperationException("Finish order is already full.");
            }

            var racer = racers[racerIndex];
            _finishOrder[FinishCount] = racerIndex;
            FinishCount++;
            racer.RecordFinished(FinishCount);
        }

        public void Restore(RaceConfiguration configuration, RaceSaveData saveData)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            Reset();
            FinishCount = saveData.FinishOrder.Count;

            for (var i = 0; i < saveData.FinishOrder.Count; i++)
            {
                var racerIndex = configuration.GetRacerIndex(saveData.FinishOrder[i]);
                if (racerIndex < 0)
                {
                    throw new InvalidOperationException($"Cannot restore unknown finisher '{saveData.FinishOrder[i]}'.");
                }

                _finishOrder[i] = racerIndex;
            }
        }

        public RacerId[] CreateFinishOrderIds(RacerRuntimeState[] racers)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            var finishOrder = new RacerId[FinishCount];
            for (var i = 0; i < FinishCount; i++)
            {
                finishOrder[i] = racers[_finishOrder[i]].Definition.Id;
            }

            return finishOrder;
        }

        public FinishedRacerResult[] CreateFinishedResults(RacerRuntimeState[] racers)
        {
            if (racers == null)
            {
                throw new ArgumentNullException(nameof(racers));
            }

            var finishers = new FinishedRacerResult[FinishCount];
            for (var i = 0; i < FinishCount; i++)
            {
                var racer = racers[_finishOrder[i]];
                finishers[i] = new FinishedRacerResult(racer.Definition.Id, racer.FinishPlacement);
            }

            return finishers;
        }

        private void Reset()
        {
            for (var i = 0; i < _finishOrder.Length; i++)
            {
                _finishOrder[i] = -1;
            }

            FinishCount = 0;
        }
    }
}
