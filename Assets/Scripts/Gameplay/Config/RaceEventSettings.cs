using System;

namespace ThreadRace.Gameplay.Config
{
    public sealed class RaceEventSettings
    {
        public const int CurrentSaveSchemaVersion = 3;
        public const string CurrentSaveKey = "ThreadRace.Save.V3";
        public const long DefaultEventDurationSeconds = 1800L;
        public const int DefaultCountdownUpdateIntervalSeconds = 1;

        public RaceEventSettings(
            RaceConfiguration raceConfiguration,
            int saveSchemaVersion,
            string saveKey,
            int defaultSeed,
            long eventDurationSeconds = DefaultEventDurationSeconds,
            int countdownUpdateIntervalSeconds = DefaultCountdownUpdateIntervalSeconds)
        {
            RaceConfiguration = raceConfiguration ?? throw new ArgumentNullException(nameof(raceConfiguration));

            if (saveSchemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(saveSchemaVersion), "Save schema version must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(saveKey))
            {
                throw new ArgumentException("Save key must not be empty.", nameof(saveKey));
            }

            if (eventDurationSeconds <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(eventDurationSeconds), "Event duration must be greater than zero seconds.");
            }

            if (countdownUpdateIntervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(countdownUpdateIntervalSeconds), "Countdown update interval must be greater than zero seconds.");
            }

            SaveSchemaVersion = saveSchemaVersion;
            SaveKey = saveKey;
            DefaultSeed = defaultSeed;
            EventDurationSeconds = eventDurationSeconds;
            CountdownUpdateIntervalSeconds = countdownUpdateIntervalSeconds;
        }

        public RaceConfiguration RaceConfiguration { get; }

        public int SaveSchemaVersion { get; }

        public string SaveKey { get; }

        public int DefaultSeed { get; }

        public long EventDurationSeconds { get; }

        public int CountdownUpdateIntervalSeconds { get; }
    }
}
