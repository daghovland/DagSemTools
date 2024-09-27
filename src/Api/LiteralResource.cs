namespace AlcTableau.Api;

/// <inheritdoc />
public class LiteralResource(string value) : Resource
{
    /// <summary>
    /// The string value of the literal. 
    /// </summary>
    public string Value { get; } = value;


    public override bool Equals(Resource? other) =>
        other != null && (ReferenceEquals(this, other) || 
                          (other is LiteralResource literal && literal.Value.Equals(Value)));

    /// <inheritdoc />
    public override string ToString() => Value;


    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
