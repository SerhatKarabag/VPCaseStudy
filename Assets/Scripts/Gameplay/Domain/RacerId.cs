using System;

namespace ThreadRace.Gameplay.Domain
{
    public readonly struct RacerId : IEquatable<RacerId>
    {
        private readonly string _value;

        public RacerId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Racer ID must not be empty.", nameof(value));
            }

            _value = value;
        }

        public string Value => _value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public bool Equals(RacerId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RacerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(RacerId left, RacerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RacerId left, RacerId right)
        {
            return !left.Equals(right);
        }
    }
}
