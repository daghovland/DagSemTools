using IriTools;
using AlcTableau.Rdf;

namespace AlcTableau.Api;

/// <summary>
/// Implementation of a rdf graph. 
/// </summary>
public class Graph : IGraph
{
    internal Graph(TripleTable triples)
    {
        Triples = triples;
    }

    private TripleTable Triples { get; init; }

    /// <inheritdoc />
    public bool isEmpty() => Triples.TripleCount == 0;

    
    private IriResource GetIriResource(uint resourceId) =>
        new IriResource(Triples.ResourceList[resourceId].iri);

    private Triple GetTriple(RDFStore.Triple triple) =>
        new Triple(GetIriResource(triple.subject),
            GetIriResource(triple.predicate).Iri,
            GetIriResource(triple.@object));
    
    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj) =>
        (Triples.ResourceMap.TryGetValue(RDFStore.Resource.NewIri(obj), out var objIdx)
         && Triples.ResourceMap.TryGetValue(RDFStore.Resource.NewIri(predicate), out var predIdx))
            ? Triples
                .GetTriplesWithObjectPredicate(objIdx, predIdx)
                .Select(GetTriple)
            : [];

    
    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate) =>
        (Triples.ResourceMap.TryGetValue(RDFStore.Resource.NewIri(subject), out var subjIdx)
         && Triples.ResourceMap.TryGetValue(RDFStore.Resource.NewIri(predicate), out var predIdx))
            ? Triples
                .GetTriplesWithSubjectPredicate(subjIdx, predIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject) =>
        (Triples.ResourceMap.TryGetValue(RDFStore.Resource.NewIri(subject), out var subjIdx))
            ? Triples
                .GetTriplesWithSubject(subjIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate) =>
        (Triples.ResourceMap.TryGetValue(RDFStore.Resource.NewIri(predicate), out var predIdx))
            ? Triples
                .GetTriplesWithPredicate(predIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithObject(IriReference @object) =>
        (Triples.ResourceMap.TryGetValue(RDFStore.Resource.NewIri(@object), out var objIdx))
            ? Triples
                .GetTriplesWithObject(objIdx)
                .Select(GetTriple)
            : [];
    
}