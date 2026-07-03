using System;

namespace ThreadRace.Gameplay.Config
{
    public sealed class AiPacingProfile
    {
        public AiPacingProfile(
            AiPacingStyle style,
            bool usesDynamicPlanning,
            float skill,
            float consistency,
            float volatility,
            float earlyPaceBias,
            float latePaceBias,
            float burstChance,
            float slumpChance,
            float finalPushChance)
        {
            Validate01(skill, nameof(skill));
            Validate01(consistency, nameof(consistency));
            Validate01(volatility, nameof(volatility));
            ValidateBias(earlyPaceBias, nameof(earlyPaceBias));
            ValidateBias(latePaceBias, nameof(latePaceBias));
            Validate01(burstChance, nameof(burstChance));
            Validate01(slumpChance, nameof(slumpChance));
            Validate01(finalPushChance, nameof(finalPushChance));

            Style = style;
            UsesDynamicPlanning = usesDynamicPlanning;
            Skill = skill;
            Consistency = consistency;
            Volatility = volatility;
            EarlyPaceBias = earlyPaceBias;
            LatePaceBias = latePaceBias;
            BurstChance = burstChance;
            SlumpChance = slumpChance;
            FinalPushChance = finalPushChance;
        }

        public AiPacingStyle Style { get; }

        public bool UsesDynamicPlanning { get; }

        public float Skill { get; }

        public float Consistency { get; }

        public float Volatility { get; }

        public float EarlyPaceBias { get; }

        public float LatePaceBias { get; }

        public float BurstChance { get; }

        public float SlumpChance { get; }

        public float FinalPushChance { get; }

        public static AiPacingProfile CreateLegacyFixed()
        {
            return new AiPacingProfile(AiPacingStyle.LegacyFixed, false, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        private static void Validate01(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentException("AI pacing value must be finite.", parameterName);
            }

            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "AI pacing value must be in the 0..1 range.");
            }
        }

        private static void ValidateBias(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentException("AI pacing bias must be finite.", parameterName);
            }

            if (value < -1f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "AI pacing bias must be in the -1..1 range.");
            }
        }
    }
}
