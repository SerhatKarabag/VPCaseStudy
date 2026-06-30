using System;

namespace ThreadRace.Gameplay.Config
{
    public sealed class AiStepTiming
    {
        public AiStepTiming(float minimumStepDelaySeconds, float maximumStepDelaySeconds)
        {
            ValidateDelay(minimumStepDelaySeconds, nameof(minimumStepDelaySeconds));
            ValidateDelay(maximumStepDelaySeconds, nameof(maximumStepDelaySeconds));

            if (minimumStepDelaySeconds > maximumStepDelaySeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumStepDelaySeconds), "AI minimum step delay must be less than or equal to the maximum step delay.");
            }

            MinimumStepDelaySeconds = minimumStepDelaySeconds;
            MaximumStepDelaySeconds = maximumStepDelaySeconds;
        }

        public float MinimumStepDelaySeconds { get; }

        public float MaximumStepDelaySeconds { get; }

        private static void ValidateDelay(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentException("AI step delay must be finite.", parameterName);
            }

            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "AI step delay must be greater than zero.");
            }
        }
    }
}
