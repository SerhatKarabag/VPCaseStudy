using System;

namespace ThreadRace.Gameplay.Application
{
    internal sealed class AiRacePlan
    {
        private readonly float[] _stepDelays;

        public AiRacePlan(float[] stepDelays)
        {
            if (stepDelays == null || stepDelays.Length == 0)
            {
                throw new ArgumentException("AI race plan requires at least one step delay.", nameof(stepDelays));
            }

            _stepDelays = new float[stepDelays.Length];
            for (var i = 0; i < stepDelays.Length; i++)
            {
                var delay = stepDelays[i];
                if (float.IsNaN(delay) || float.IsInfinity(delay) || delay <= 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(stepDelays), "AI race plan delay must be finite and greater than zero.");
                }

                _stepDelays[i] = delay;
            }
        }

        public float GetDelayForNextStep(int completedStepCount)
        {
            if (completedStepCount < 0 || completedStepCount >= _stepDelays.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(completedStepCount), "Completed step count is outside the AI race plan.");
            }

            return _stepDelays[completedStepCount];
        }
    }
}
