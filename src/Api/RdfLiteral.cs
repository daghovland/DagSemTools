namespace DagSemTools.Api;

/// <inheritdoc />
public class RdfLiteral(string value) : GraphElement
{
    /// <summary>
    /// The string value of the literal. 
    /// </summary>
    public string Value { get; } = value;


    /// <summary>
    /// Two literals are equal if their string values are equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(GraphElement? other) =>
        other != null && (ReferenceEquals(this, other) ||
                          (other is RdfLiteral literal && literal.Value.Equals(Value)));

    /// <inheritdoc />
    public override string ToString() => Value;


    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
