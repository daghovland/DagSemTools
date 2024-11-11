using IriTools;
using DagSemTools.Rdf;

namespace DagSemTools.Api;

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

    /// <summary>
    /// Loads and runs datalog rules from the file
    /// Note that this adds new triples to the datastore
    /// </summary>
    /// <param name="datalog">The file with the datalog program</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void LoadDatalog(FileInfo datalog);

    /// <summary>
    /// Enables OWL 2 RL Reasoning
    /// https://www.w3.org/TR/owl2-profiles/#Reasoning_in_OWL_2_RL_and_RDF_Graphs_using_Rules
    /// Note that this adds new triples to the datastore
    /// </summary>
    public void EnableOwlReasoning();

}