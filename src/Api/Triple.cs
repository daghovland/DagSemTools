using IriTools;
using DagSemTools.Rdf;

namespace DagSemTools.Api;

/// <summary>
/// Represents a triple in RDF. https://www.w3.org/TR/rdf12-concepts/#dfn-rdf-triple
/// </summary>
public class Triple
{
    /// <summary>
    /// The most generic triple constructor. 
    /// </summary>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <param name="object"></param>
    public Triple(Resource subject, IriReference predicate, GraphElement @object)
    {
        Subject = subject;
        Predicate = predicate;
        Object = @object;
    }

    /// <summary>
    /// Creates a triple with IRIs on all three places
    /// </summary>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <param name="object"></param>
    public Triple(IriReference subject, IriReference predicate, IriReference @object)
    {
        Subject = new IriResource(subject);
        Predicate = predicate;
        Object = new IriResource(@object);
    }

    /// <summary>
    /// The subject of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-subject
    /// </summary>
    public Resource Subject { get; }

    /// <summary>
    /// The predicate of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-predicate
    /// </summary>
    public IriReference Predicate { get; }

    /// <summary>
    /// The object of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-object
    /// </summary>
    public GraphElement Object { get; }

}

