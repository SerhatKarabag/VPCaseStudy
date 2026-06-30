using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThreadRace.Core.Random;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Persistence
{
    public sealed class RaceSaveData
    {
        private readonly ReadOnlyCollection<RaceSaveRacerData> _racers;
        private readonly ReadOnlyCollection<RacerId> _finishOrder;

        public RaceSaveData(
            int schemaVersion,
            RacePhase phase,
            IEnumerable<RaceSaveRacerData> racers,
            IEnumerable<RacerId> finishOrder,
            RaceSavePlayerOutcomeData playerOutcome,
            DeterministicRandomState randomState,
            int revision)
        {
            if (schemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Save schema version must be greater than zero.");
            }

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), "Save revision must not be negative.");
            }

            SchemaVersion = schemaVersion;
            Phase = phase;
            _racers = Array.AsReadOnly(CopyRequired(racers, nameof(racers)));
            _finishOrder = Array.AsReadOnly(CopyRequired(finishOrder, nameof(finishOrder)));
            PlayerOutcome = playerOutcome;
            RandomState = randomState;
            Revision = revision;
        }

        public int SchemaVersion { get; }

        public RacePhase Phase { get; }

        public IReadOnlyList<RaceSaveRacerData> Racers => _racers;

        public IReadOnlyList<RacerId> FinishOrder => _finishOrder;

        public RaceSavePlayerOutcomeData PlayerOutcome { get; }

        public DeterministicRandomState RandomState { get; }

        public int Revision { get; }

        private static T[] CopyRequired<T>(IEnumerable<T> source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copied = new List<T>();
            foreach (var item in source)
            {
                if (item == null)
                {
                    throw new ArgumentException("Save collections must not contain null entries.", parameterName);
                }

                copied.Add(item);
            }

            return copied.ToArray();
        }
    }
}
