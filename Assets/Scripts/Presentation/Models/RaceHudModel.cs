using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThreadRace.Presentation.Models
{
    public sealed class RaceHudModel
    {
        private readonly ReadOnlyCollection<RacerHudRowModel> _rows;

        public RaceHudModel(IEnumerable<RacerHudRowModel> rows, string countdownText = "")
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            CountdownText = countdownText ?? string.Empty;

            var copied = new List<RacerHudRowModel>();
            foreach (var row in rows)
            {
                if (row == null)
                {
                    throw new ArgumentException("HUD rows must not contain null entries.", nameof(rows));
                }

                copied.Add(row);
            }

            _rows = Array.AsReadOnly(copied.ToArray());
        }

        public IReadOnlyList<RacerHudRowModel> Rows => _rows;

        public string CountdownText { get; }
    }
}
