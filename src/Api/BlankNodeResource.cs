namespace AlcTableau.Api;

/// <summary>
/// Represents a blank node resource.
/// </summary>
public class BlankNodeResource(string name) : BlankNodeOrIriResource
{
    internal string Name { get; } = name;

    public override bool Equals(Resource? other) =>
        other != null && (ReferenceEquals(this, other) || other is BlankNodeResource bnr && bnr.Name == Name);
    
    public override bool Equals(object? other) =>
        other != null && other is Resource bnr && Equals(bnr);

    /// <inheritdoc />
    public override string ToString() => $"_:{Name}";
}