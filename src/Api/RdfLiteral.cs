namespace DagSemTools.Api;

/// <inheritdoc />
public abstract class RdfLiteral(Ingress.RdfLiteral rdfLiteral) : GraphElement
{
    internal readonly Ingress.RdfLiteral InternalRdfLiteral = rdfLiteral;
    
    /// <summary>
    /// Two literals are equal if their string values are equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(GraphElement? other) =>
        other != null && (ReferenceEquals(this, other) ||
                          (other is RdfLiteral literal && literal.InternalRdfLiteral.Equals(InternalRdfLiteral)));

    /// <inheritdoc />
    public override string ToString() => InternalRdfLiteral.ToString();


    /// <inheritdoc />
    public override int GetHashCode() => InternalRdfLiteral.GetHashCode();
}
