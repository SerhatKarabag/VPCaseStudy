using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Presentation.Models
{
    public static class RaceCountdownFormatter
    {
        private const long SecondsPerMinute = 60L;
        private const long SecondsPerHour = 60L * SecondsPerMinute;
        private const long SecondsPerDay = 24L * SecondsPerHour;

        public static string FormatCountdown(long remainingSeconds)
        {
            if (remainingSeconds <= 0L)
            {
                return "00:00";
            }

            if (remainingSeconds >= SecondsPerDay)
            {
                var days = remainingSeconds / SecondsPerDay;
                var hours = remainingSeconds % SecondsPerDay / SecondsPerHour;
                return days.ToString() + "d " + hours.ToString("00") + "h";
            }

            if (remainingSeconds >= SecondsPerHour)
            {
                var hours = remainingSeconds / SecondsPerHour;
                var minutes = remainingSeconds % SecondsPerHour / SecondsPerMinute;
                return hours.ToString("00") + "h " + minutes.ToString("00") + "m";
            }

            var remainingMinutes = remainingSeconds / SecondsPerMinute;
            var seconds = remainingSeconds % SecondsPerMinute;
            if (remainingMinutes <= 0L)
            {
                return seconds.ToString() + "s";
            }

            if (seconds == 0L)
            {
                return remainingMinutes.ToString() + "m";
            }

            return remainingMinutes.ToString() + "m " + seconds.ToString("00") + "s";
        }

        public static string FormatCompactCountdown(RaceCountdownSnapshot countdown)
        {
            if (countdown == null || (!countdown.IsActive && !countdown.IsExpired))
            {
                return "--";
            }

            return countdown.IsExpired || countdown.RemainingSeconds <= 0L
                ? "ENDED"
                : FormatCountdown(countdown.RemainingSeconds);
        }

        public static string FormatCompactCountdown(RaceCountdownSnapshot countdown, RacePhase phase)
        {
            return phase == RacePhase.Reward || phase == RacePhase.Completed
                ? "ENDED"
                : FormatCompactCountdown(countdown);
        }

        public static string FormatTimeLeftLine(RaceCountdownSnapshot countdown)
        {
            return "TIME LEFT: " + FormatCompactCountdown(countdown);
        }

        public static string FormatDurationLine(long durationSeconds)
        {
            if (durationSeconds <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be positive.");
            }

            if (durationSeconds % SecondsPerDay == 0L)
            {
                var days = durationSeconds / SecondsPerDay;
                return "EVENT DURATION: " + days.ToString() + (days == 1L ? " DAY" : " DAYS");
            }

            if (durationSeconds % SecondsPerMinute == 0L && durationSeconds < SecondsPerHour)
            {
                return "EVENT DURATION: " + (durationSeconds / SecondsPerMinute).ToString() + " MIN";
            }

            return "EVENT DURATION: " + FormatCountdown(durationSeconds).ToUpperInvariant();
        }
    }
}
