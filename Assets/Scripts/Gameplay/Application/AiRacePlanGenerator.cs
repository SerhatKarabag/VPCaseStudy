using System;
using ThreadRace.Gameplay.Config;

namespace ThreadRace.Gameplay.Application
{
    internal static class AiRacePlanGenerator
    {
        private const float MinimumDelaySeconds = 0.05f;

        public static AiRacePlan Generate(RacerDefinition racer, int finishTarget, int eventSeed)
        {
            if (racer == null)
            {
                throw new ArgumentNullException(nameof(racer));
            }

            if (finishTarget <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finishTarget));
            }

            if (racer.AiStepTiming == null)
            {
                throw new InvalidOperationException($"AI racer '{racer.Id}' is missing step timing.");
            }

            var delays = new float[finishTarget];
            var profile = racer.AiPacingProfile ?? AiPacingProfile.CreateLegacyFixed();
            for (var stepIndex = 0; stepIndex < delays.Length; stepIndex++)
            {
                delays[stepIndex] = GenerateDelay(racer, profile, finishTarget, stepIndex, eventSeed);
            }

            return new AiRacePlan(delays);
        }

        private static float GenerateDelay(
            RacerDefinition racer,
            AiPacingProfile profile,
            int finishTarget,
            int stepIndex,
            int eventSeed)
        {
            var timing = racer.AiStepTiming;
            if (!profile.UsesDynamicPlanning)
            {
                return GenerateLegacyDelay(racer, timing, stepIndex, eventSeed);
            }

            var minimum = timing.MinimumStepDelaySeconds;
            var maximum = timing.MaximumStepDelaySeconds;
            var average = (minimum + maximum) * 0.5f;
            var range = maximum - minimum;
            var progressRatio = finishTarget <= 1 ? 1f : stepIndex / (float)(finishTarget - 1);
            var racerHash = HashRacerId(racer.Id.Value);

            var eventForm = Lerp(
                -0.11f - profile.Volatility * 0.05f,
                0.11f + profile.Volatility * 0.05f,
                Sample01(eventSeed, racerHash, 997));
            var jitterWidth = range * Lerp(0.12f, 0.72f, (1f - profile.Consistency) * 0.55f + profile.Volatility * 0.45f);
            var jitter = (Sample01(eventSeed, racerHash, stepIndex * 17 + 11) - 0.5f) * jitterWidth;

            var delay = average + jitter;
            delay *= 1f - ((profile.Skill - 0.5f) * 0.16f);
            delay *= 1f - eventForm;

            var paceBias = (profile.EarlyPaceBias * (1f - progressRatio)) + (profile.LatePaceBias * progressRatio);
            delay *= 1f - (paceBias * 0.16f);

            var eventSample = Sample01(eventSeed, racerHash, stepIndex * 31 + 19);
            if (eventSample < profile.BurstChance)
            {
                delay *= Lerp(0.82f, 0.58f, profile.Volatility);
            }
            else if (eventSample > 1f - profile.SlumpChance)
            {
                delay *= Lerp(1.12f, 1.48f, profile.Volatility);
            }

            if (progressRatio >= 0.72f && Sample01(eventSeed, racerHash, stepIndex * 43 + 23) < profile.FinalPushChance)
            {
                delay *= Lerp(0.92f, 0.76f, profile.Volatility);
            }

            var lowerBound = Math.Max(MinimumDelaySeconds, minimum * 0.62f);
            var upperBound = Math.Max(lowerBound, maximum * (1.22f + profile.Volatility * 0.28f));
            return Clamp(delay, lowerBound, upperBound);
        }

        private static float GenerateLegacyDelay(RacerDefinition racer, AiStepTiming timing, int stepIndex, int eventSeed)
        {
            if (timing.MinimumStepDelaySeconds == timing.MaximumStepDelaySeconds)
            {
                return timing.MinimumStepDelaySeconds;
            }

            var sample = Sample01(eventSeed, HashRacerId(racer.Id.Value), stepIndex * 17 + 5);
            return Lerp(timing.MinimumStepDelaySeconds, timing.MaximumStepDelaySeconds, sample);
        }

        private static uint HashRacerId(string racerId)
        {
            unchecked
            {
                var hash = 2166136261u;
                for (var i = 0; i < racerId.Length; i++)
                {
                    hash ^= racerId[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private static float Sample01(int seed, uint racerHash, int salt)
        {
            unchecked
            {
                var value = (uint)seed;
                value ^= racerHash + 0x9E3779B9u + (value << 6) + (value >> 2);
                value ^= (uint)salt * 0x85EBCA6Bu;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return value / ((float)uint.MaxValue + 1f);
            }
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + ((to - from) * Clamp01(t));
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
