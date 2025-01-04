namespace DagSemTools.Api;

/// <inheritdoc />
public class RdfLiteral(Ingress.RdfLiteral rdfLiteral) : GraphElement
{
    internal readonly Ingress.RdfLiteral InternalRdfLiteral = rdfLiteral;

    /// <summary>
    /// Creates an rdf literal of type xsd:string. This is the default type in rdf
    /// </summary>
    /// <param name="rdfLiteral"></param>
    /// <returns></returns>
    public static RdfLiteral StringRdfLiteral(string rdfLiteral) =>
        new RdfLiteral(DagSemTools.Ingress.RdfLiteral.NewLiteralString(rdfLiteral));
    
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
