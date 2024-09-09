namespace Api;
using IriTools;

    /// <summary>
    /// Represents a resource that is identified by an IRI.
    /// </summary>
    public class IriResource : Resource
    {
        /// <summary>
        /// The IRI that identifies the resource.
        /// </summary>
        public IriReference Iri { get; }

        /// <inheritdoc />
        public IriResource(IriReference iri)
        {
            Iri = iri ?? throw new ArgumentNullException(nameof(iri));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Iri.ToString();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is IriResource other)
            {
                return Iri.Equals(other.Iri);
            }
            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Iri.GetHashCode();
        }
    }
