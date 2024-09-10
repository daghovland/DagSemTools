namespace AlcTableau.Api;

/// <inheritdoc />
public class LiteralResource : Resource
    {
        /// <summary>
        /// The string value of the literal. 
        /// </summary>
        public string Value { get; }

        /// <inheritdoc />
        public LiteralResource(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj != null && obj is LiteralResource other)
            {
                return Value.Equals(other.Value);
            }
            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
