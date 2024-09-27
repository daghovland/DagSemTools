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


    private BlankNodeOrIriResource GetBlankNodeOrIriResource(uint resourceId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(resourceId, Triples.ResourceCount);
        var resource = Triples.ResourceList[resourceId];

        switch (resource)
        {
            case RDFStore.Resource { IsIri: true } r:
                return new IriResource(new IriReference(r.iri));
            case var r when r.IsAnonymousBlankNode:
                return new BlankNodeResource($"{r.anon_blankNode}");
            case var r when r.IsNamedBlankNode:
                return new BlankNodeResource($"{r.blankNode}");
            default: throw new ArgumentException($"Resource {resource.ToString()} is not an Iri or a blank node"); ;
        }
    }


    private IriResource GetIriResource(uint resourceId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(resourceId, Triples.ResourceCount);
        var resource = Triples.ResourceList[resourceId];
        if (!resource.IsIri)
            throw new ArgumentException($"Resource {resource.ToString()} is not an Iri");
        return new IriResource(new IriReference(resource.iri));

    }

    private Resource GetResource(uint resourceId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(resourceId, Triples.ResourceCount);
        var resource = Triples.ResourceList[resourceId];
        switch (resource)
        {
            case RDFStore.Resource { IsIri: true } r:
                return new IriResource(new IriReference(r.iri));
            case var r when r.IsAnonymousBlankNode:
                return new BlankNodeResource($"{r.anon_blankNode}");
            case var r when r.IsNamedBlankNode:
                return new BlankNodeResource($"{r.blankNode}");
            case var r when r.IsLangLiteral:
                return new LiteralResource(r.langliteral);
            case RDFStore.Resource { IsDateLiteral: true } r:
                return new LiteralResource(r.literalDate.ToString());
            case RDFStore.Resource { IsLiteralString: true } r:
                return new LiteralResource(r.literal);
            default: throw new NotImplementedException("Literal type not implemented. Sorry");
        }
    }

    private Triple GetTriple(RDFStore.Triple triple) =>
    new Triple(this.GetBlankNodeOrIriResource(triple.subject),
            GetIriResource(triple.predicate).Iri,
            GetResource(triple.@object));

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