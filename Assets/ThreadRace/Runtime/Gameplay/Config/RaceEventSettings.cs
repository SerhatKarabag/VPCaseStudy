using System;

namespace ThreadRace.Gameplay.Config
{
    public sealed class RaceEventSettings
    {
        public RaceEventSettings(
            RaceConfiguration raceConfiguration,
            int saveSchemaVersion,
            string saveKey,
            int defaultSeed)
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

            SaveSchemaVersion = saveSchemaVersion;
            SaveKey = saveKey;
            DefaultSeed = defaultSeed;
        }

        public RaceConfiguration RaceConfiguration { get; }

        public int SaveSchemaVersion { get; }

        public string SaveKey { get; }

        public int DefaultSeed { get; }
    }
}
