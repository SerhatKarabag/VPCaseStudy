using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Config
{
    public sealed class RacerDefinition
    {
        public RacerDefinition(
            RacerId id,
            string displayName,
            RacerType racerType,
            int initialOrder,
            AiStepTiming aiStepTiming,
            AiPacingProfile aiPacingProfile = null)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Racer definition requires a valid racer ID.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Racer display name must not be empty.", nameof(displayName));
            }

            if (racerType != RacerType.Player && racerType != RacerType.Ai)
            {
                throw new ArgumentOutOfRangeException(nameof(racerType), "Unsupported racer type.");
            }

            if (initialOrder < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialOrder), "Initial order must not be negative.");
            }

            if (racerType == RacerType.Ai && aiStepTiming == null)
            {
                throw new ArgumentNullException(nameof(aiStepTiming), "AI racers require step timing.");
            }

            if (racerType == RacerType.Ai && aiPacingProfile == null)
            {
                throw new ArgumentNullException(nameof(aiPacingProfile), "AI racers require a pacing profile.");
            }

            if (racerType == RacerType.Player && aiStepTiming != null)
            {
                throw new ArgumentException("Player racers must not define AI step timing.", nameof(aiStepTiming));
            }

            if (racerType == RacerType.Player && aiPacingProfile != null)
            {
                throw new ArgumentException("Player racers must not define an AI pacing profile.", nameof(aiPacingProfile));
            }

            Id = id;
            DisplayName = displayName;
            RacerType = racerType;
            InitialOrder = initialOrder;
            AiStepTiming = aiStepTiming;
            AiPacingProfile = aiPacingProfile;
        }

        public RacerId Id { get; }

        public string DisplayName { get; }

        public RacerType RacerType { get; }

        public int InitialOrder { get; }

        public AiStepTiming AiStepTiming { get; }

        public AiPacingProfile AiPacingProfile { get; }

        public static RacerDefinition CreatePlayer(string id, string displayName, int initialOrder)
        {
            return new RacerDefinition(new RacerId(id), displayName, RacerType.Player, initialOrder, null);
        }

        public static RacerDefinition CreateAi(string id, string displayName, int initialOrder, float minimumStepDelaySeconds, float maximumStepDelaySeconds)
        {
            return CreateAi(
                id,
                displayName,
                initialOrder,
                minimumStepDelaySeconds,
                maximumStepDelaySeconds,
                AiPacingProfile.CreateLegacyFixed());
        }

        public static RacerDefinition CreateAi(
            string id,
            string displayName,
            int initialOrder,
            float minimumStepDelaySeconds,
            float maximumStepDelaySeconds,
            AiPacingProfile pacingProfile)
        {
            return new RacerDefinition(
                new RacerId(id),
                displayName,
                RacerType.Ai,
                initialOrder,
                new AiStepTiming(minimumStepDelaySeconds, maximumStepDelaySeconds),
                pacingProfile);
        }
    }
}
