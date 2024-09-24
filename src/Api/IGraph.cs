using IriTools;
using AlcTableau.Rdf;

namespace AlcTableau.Api;

/// <summary>
/// Represents an RDF Graph https://www.w3.org/TR/rdf12-concepts/
/// </summary>
public interface IGraph
{
    /// <summary>
    /// Returns true if the graph is empty, that is, has no triples.
    /// </summary>
    /// <returns></returns>
    public bool isEmpty();
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given predicate and object.
    /// Similar to the sparql query "SELECT * WHERE { ?s predicate obj }".
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given subject and predicate.
    /// Similar to the sparql query "SELECT * WHERE { subject predicate ?o }".
    /// </summary>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given subject.
    /// Similar to the sparql query "SELECT * WHERE { subject ?p ?o }".
    /// </summary>
    /// <param name="subject"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given predicate.
    /// Similar to the sparql query "SELECT * WHERE { ?s predicate ?o }".
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given object.
    /// Similar to the sparql query "SELECT * WHERE { ?s ?p obj }".
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithObject(IriReference obj);
}