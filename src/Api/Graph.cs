using IriTools;
using Rdf;

namespace Api;

/// <summary>
/// Implementation of a rdf graph. 
/// </summary>
public class Graph : IGraph
{
    internal Graph(TripleTable triples)
    {
        Triples = triples;
    }
internal TripleTable Triples { get; init; }

/// <inheritdoc />
public bool isEmpty() => Triples.TripleCount == 0;

/// <inheritdoc />
public IEnumerator<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerator<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerator<Triple> GetTriplesWithSubject(IriReference subject)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerator<Triple> GetTriplesWithPredicate(IriReference predicate)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerator<Triple> GetTriplesWithObject(IriReference @object)
    {
        throw new NotImplementedException();
    }
}