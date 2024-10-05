using DagSemTools.Rdf;

namespace DagSemTools.Api
{
    /// <summary>
    /// Represents an RDF resource. https://www.w3.org/TR/rdf12-concepts/#resources-and-statements
    /// </summary>
    public abstract class Resource : IEquatable<Resource>, IComparable<Resource>, IComparable
    {
        /// <inheritdoc />
        public abstract bool Equals(Resource? other);


        /// <summary>
        /// Compares to Resources based on the string representation in ToString()
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Resource? other) =>
            other switch
            {
                null => 1,
                _ => string.Compare(ToString(), other.ToString(), StringComparison.Ordinal)
            };

        /// <inheritdoc />
        public abstract override string ToString();

        /// <summary>
        /// Compares to resources based on the string representation in ToString()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public int CompareTo(object? obj) =>
            obj switch
            {
                null => 1,
                Resource iri => string.Compare(ToString(), iri.ToString(), StringComparison.Ordinal),
                _ => throw new ArgumentException("Object is not a Resource")
            };

    }
}