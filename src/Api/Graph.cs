using DagSemTools.Datalog;
using IriTools;
using DagSemTools.Rdf;
using Microsoft.FSharp.Collections;
using DagSemTools.Resource;

namespace DagSemTools.Api;

/// <summary>
/// Implementation of a rdf graph. 
/// </summary>
public class Graph : IGraph
{
    internal Graph(Datastore triples)
    {
        Triples = triples;
    }

    private Datastore Triples { get; init; }

    private IEnumerable<Rule> _rules = Enumerable.Empty<Rule>();

    /// <summary>
    /// Loads and runs datalog rules from the file
    /// Note that this adds new triples to the datastore
    /// </summary>
    /// <param name="datalog">The file with the datalog program</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void LoadDatalog(FileInfo datalog)
    {
        var newRules = Datalog.Parser.Parser.ParseFile(datalog, System.Console.Error,
            Triples ?? throw new InvalidOperationException());
        _rules = _rules.Concat(newRules);
        Reasoner.evaluate(ListModule.OfSeq(_rules), Triples);
    }


    /// <inheritdoc />
    public bool isEmpty() => Triples.Triples.TripleCount == 0;


    private BlankNodeOrIriResource GetBlankNodeOrIriResource(uint resourceId)
    {
        var resource = Triples.GetResource(resourceId);
        switch (resource)
        {
            case DagSemTools.Resource.Resource { IsIri: true } r:
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
        var resource = Triples.GetResource(resourceId);
        if (!resource.IsIri)
            throw new ArgumentException($"Resource {resource.ToString()} is not an Iri");
        return new IriResource(new IriReference(resource.iri));

    }

    private Resource GetResource(uint resourceId)
    {
        var resource = Triples.GetResource(resourceId);
        switch (resource)
        {
            case DagSemTools.Resource.Resource { IsIri: true } r:
                return new IriResource(new IriReference(r.iri));
            case var r when r.IsAnonymousBlankNode:
                return new BlankNodeResource($"{r.anon_blankNode}");
            case var r when r.IsNamedBlankNode:
                return new BlankNodeResource($"{r.blankNode}");
            case var r when r.IsLangLiteral:
                return new LiteralResource(r.langliteral);
            case DagSemTools.Resource.Resource { IsDateLiteral: true } r:
                return new LiteralResource(r.literalDate.ToString());
            case DagSemTools.Resource.Resource { IsLiteralString: true } r:
                return new LiteralResource(r.literal);
            default: throw new NotImplementedException("Literal type not implemented. Sorry");
        }
    }

    private Triple GetTriple(Ingress.Triple triple) =>
    new Triple(this.GetBlankNodeOrIriResource(triple.subject),
            GetIriResource(triple.predicate).Iri,
            GetResource(triple.obj));

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj) =>
        (Triples.Resources.ResourceMap.TryGetValue(DagSemTools.Resource.Resource.NewIri(obj), out var objIdx)
         && Triples.Resources.ResourceMap.TryGetValue(DagSemTools.Resource.Resource.NewIri(predicate), out var predIdx))
            ? Triples
                .GetTriplesWithObjectPredicate(objIdx, predIdx)
                .Select(GetTriple)
            : [];


    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate) =>
        (Triples.Resources.ResourceMap.TryGetValue(DagSemTools.Resource.Resource.NewIri(subject), out var subjIdx)
         && Triples.Resources.ResourceMap.TryGetValue(DagSemTools.Resource.Resource.NewIri(predicate), out var predIdx))
            ? Triples
                .GetTriplesWithSubjectPredicate(subjIdx, predIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject) =>
        (Triples.Resources.ResourceMap.TryGetValue(DagSemTools.Resource.Resource.NewIri(subject), out var subjIdx))
            ? Triples
                .GetTriplesWithSubject(subjIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate) =>
        (Triples.Resources.ResourceMap.TryGetValue(DagSemTools.Resource.Resource.NewIri(predicate), out var predIdx))
            ? Triples
                .GetTriplesWithPredicate(predIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithObject(IriReference @object) =>
        (Triples.Resources.ResourceMap.TryGetValue(DagSemTools.Resource.Resource.NewIri(@object), out var objIdx))
            ? Triples
                .GetTriplesWithObject(objIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public void EnableOwlReasoning()
    {
        _rules = DagSemTools.OWL2RL2Datalog.Reasoner.enableEqualityReasoning(Triples, _rules, Console.Error);
        Reasoner.evaluate(ListModule.Empty<Rule>(), Triples);
    }
}