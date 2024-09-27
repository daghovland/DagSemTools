namespace AlcTableau.Api;

/// <summary>
/// Represents a blank node resource.
/// </summary>
public class BlankNodeResource(string name) : BlankNodeOrIriResource
{
    internal string Name { get; } = name;

    /// <summary>
    /// Blank nodes are equal if they have the same name.
    /// This comparison of course only makes sense inside a single RDF graph.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(Resource? other) =>
        other != null && (ReferenceEquals(this, other) || other is BlankNodeResource bnr && bnr.Name == Name);

    /// <summary>
    /// Blank nodes are equal if they have the same name.
    /// This comparison of course only makes sense inside a single RDF graph.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(object? other) =>
        other != null && other is Resource bnr && Equals(bnr);

    /// <inheritdoc />
    public override string ToString() => $"_:{Name}";

    /// <summary>
    /// The hash code is made from the blank node name. 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => name.GetHashCode();
}