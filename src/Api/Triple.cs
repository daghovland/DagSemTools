using IriTools;
using AlcTableau.Rdf;

namespace AlcTableau.Api;

/// <summary>
/// Represents a triple in RDF. https://www.w3.org/TR/rdf12-concepts/#dfn-rdf-triple
/// </summary>
public class Triple(IriResource subject, IriReference predicate, Resource @object)
{
    /// <summary>
    /// The subject of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-subject
    /// </summary>
    public IriResource Subject { get; } = subject;
    
    /// <summary>
    /// The predicate of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-predicate
    /// </summary>
    public IriReference Predicate { get; } = predicate;
    
    /// <summary>
    /// The object of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-object
    /// </summary>
    public Resource Object { get; } = @object;
}