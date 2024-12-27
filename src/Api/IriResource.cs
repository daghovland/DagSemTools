namespace DagSemTools.Api;
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

    /// <summary>
    /// Two Iri resources are equal if their IRIs are equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(GraphElement? other) =>
        other != null && (ReferenceEquals(this, other) ||
                          (other is IriResource iri && iri.Iri.Equals(Iri)));


    /// <inheritdoc />
    public override string ToString()
    {
        return Iri.ToString();
    }

    /// <summary>
    /// Two Iri resources are equal if their IRIs are equal.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) =>
        obj != null && obj is GraphElement r && Equals(r);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Iri.GetHashCode();
    }
}
