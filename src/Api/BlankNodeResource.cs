namespace DagSemTools.Api;

/// <summary>
/// Represents a blank node resource.
/// </summary>
public class BlankNodeResource(string name) : BlankNodeOrIriResource
{
    private readonly string _name = name;

    /// <summary>
    /// Blank nodes are equal if they have the same name.
    /// This comparison of course only makes sense inside a single RDF graph.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(Resource? other) =>
        other != null && (ReferenceEquals(this, other) || other is BlankNodeResource bnr && bnr._name == _name);

    /// <summary>
    /// Blank nodes are equal if they have the same name.
    /// This comparison of course only makes sense inside a single RDF graph.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(object? other) =>
        other != null && other is Resource bnr && Equals(bnr);

    /// <inheritdoc />
    public override string ToString() => $"_:{_name}";

    /// <summary>
    /// The hash code is made from the blank node name. 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _name.GetHashCode();
}