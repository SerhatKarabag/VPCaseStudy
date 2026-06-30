using System;

namespace ThreadRace.Gameplay.Application
{
    public sealed class RaceControllerInitializationResult
    {
        private RaceControllerInitializationResult(RaceControllerInitializationStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public RaceControllerInitializationStatus Status { get; }

        public string Message { get; }

        public bool CanStartNewRace =>
            Status == RaceControllerInitializationStatus.NoSave
            || Status == RaceControllerInitializationStatus.Restored
            || Status == RaceControllerInitializationStatus.Reset;

        public static RaceControllerInitializationResult NoSave()
        {
            return new RaceControllerInitializationResult(RaceControllerInitializationStatus.NoSave, null);
        }

        public static RaceControllerInitializationResult Restored()
        {
            return new RaceControllerInitializationResult(RaceControllerInitializationStatus.Restored, null);
        }

        public static RaceControllerInitializationResult Reset()
        {
            return new RaceControllerInitializationResult(RaceControllerInitializationStatus.Reset, null);
        }

        public static RaceControllerInitializationResult InvalidSave(string message)
        {
            return CreateFailure(RaceControllerInitializationStatus.InvalidSave, message);
        }

        public static RaceControllerInitializationResult LoadFailed(string message)
        {
            return CreateFailure(RaceControllerInitializationStatus.LoadFailed, message);
        }

        private static RaceControllerInitializationResult CreateFailure(
            RaceControllerInitializationStatus status,
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Failure results require a message.", nameof(message));
            }

            return new RaceControllerInitializationResult(status, message);
        }
    }
}
