using IriTools;
using DagSemTools.Rdf;

namespace DagSemTools.Api;

/// <summary>
/// Represents a triple in RDF. https://www.w3.org/TR/rdf12-concepts/#dfn-rdf-triple
/// </summary>
public class Triple(Resource subject, IriReference predicate, GraphElement @object)
{

    /// <summary>
    /// The subject of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-subject
    /// </summary>
    public Resource Subject { get; } = subject;

    /// <summary>
    /// The predicate of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-predicate
    /// </summary>
    public IriReference Predicate { get; } = predicate;

    /// <summary>
    /// The object of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-object
    /// </summary>
    public GraphElement Object { get; } = @object;

}

