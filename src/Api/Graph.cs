using DagSemTools.Datalog;
using IriTools;
using DagSemTools.Rdf;
using Microsoft.FSharp.Collections;
using DagSemTools.Ingress;
using LanguageExt;
using Microsoft.FSharp.Core;
using Serilog;

namespace DagSemTools.Api;

/// <summary>
/// Implementation of a rdf graph. 
/// </summary>
public class Graph : IGraph
{
    private ILogger _logger;
    internal Graph(Datastore triples, ILogger? logger = null)
    {
        Triples = triples;
        _logger = logger ?? new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }

    private Datastore Triples { get; init; }

    private IEnumerable<Rule> _rules = Enumerable.Empty<Rule>();

    /// <inheritdoc />
    public bool ContainsTriple(Triple apiTriple) =>
        TryGetRdfTriple(apiTriple, out var rdfTriple)
         && Triples
             .ContainsTriple(rdfTriple);

    private bool GetRdfIriGraphElementId(IriReference subject, out uint subjIdx) =>
        Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(subject)),
            out subjIdx);

    private bool GetRdfLiteralGraphElementId(RdfLiteral literal, out uint subjIdx) =>
        Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewGraphLiteral(literal.InternalRdfLiteral), out subjIdx);

    private bool GetRdfResourceGraphElementId(Resource resource, out uint idx) =>
        resource switch
        {
            BlankNodeResource blankNodeResource => throw new NotImplementedException(),
            IriResource iriResource => GetRdfIriGraphElementId(iriResource.Iri, out idx),
            _ => throw new Exception($"Unknown resource type: {resource.GetType().FullName}"),
        };
    private bool GetRdfGraphElementId(GraphElement gel, out uint idx) =>
        gel switch
        {
            Resource resource => GetRdfResourceGraphElementId(resource, out idx),
            RdfLiteral literal => GetRdfLiteralGraphElementId(literal, out idx),
            _ => throw new Exception($"Unknown resource type: {gel.GetType().FullName}"),
        };
    internal bool TryGetRdfTriple(Triple apiTriple, out Rdf.Ingress.Triple rdfTriple)
    {
        if (GetRdfResourceGraphElementId(apiTriple.Subject, out var subjIdx) &&
            GetRdfIriGraphElementId(apiTriple.Predicate, out var predIdx) &&
            GetRdfGraphElementId(apiTriple.Object, out var objIdx))
        {
            rdfTriple = new Rdf.Ingress.Triple(subjIdx, predIdx, objIdx);
            return true;
        }

        rdfTriple = default;
        return false;
    }

    internal Rdf.Ingress.Triple EnsureRdfTriple(Triple apiTriple) =>
        (GetRdfResourceGraphElementId(apiTriple.Subject, out var subjIdx) &&
            GetRdfIriGraphElementId(apiTriple.Predicate, out var predIdx) &&
            GetRdfGraphElementId(apiTriple.Object, out var objIdx)) ?
        new Rdf.Ingress.Triple(subjIdx, predIdx, objIdx) :
        throw new Exception($"BUG: Something went wrong when translating {apiTriple}");

    /// <inheritDoc />
    public void LoadDatalog(FileInfo datalog)
    {
        var newRules = Datalog.Parser.Parser.ParseFile(datalog, System.Console.Error,
            Triples ?? throw new InvalidOperationException());
        LoadDatalog(newRules);
    }

    /// <inheritdoc />
    public IEnumerable<Dictionary<string, GraphElement>> AnswerSelectQuery(string query)
    {
        var parsedQuery = Sparql.Parser.Parser.ParseString(query, Console.Error, Triples.Resources);
        var results = QueryProcessor.Answer(Triples, parsedQuery.Item1);
        return results
            .Map(r =>
                r.ToDictionary(kv => kv.Key, kv => GetResource(kv.Value)))
            .ToList();
    }

    /// <inheritDoc />
    public void LoadDatalog(IEnumerable<Rule> newRules)
    {
        _rules = _rules.Concat(newRules);
        Reasoner.evaluate(_logger, ListModule.OfSeq(_rules), Triples);
    }

    /// <inheritdoc />
    public bool IsEmpty() => Triples.Triples.TripleCount == 0;


    private Resource GetBlankNodeOrIriResource(uint resourceId)
    {
        var resource = Triples.GetGraphNode(resourceId);
        if (!FSharpOption<RdfResource>.get_IsSome(resource))
            throw new ArgumentException($"Resource {resource} is not an Iri or a blank node"); ;

        switch (resource.Value)
        {
            case { IsIri: true } r:
                return new IriResource(new IriReference(r.iri));
            case { IsAnonymousBlankNode: true } r:
                return new BlankNodeResource($"{r.anon_blankNode}");
            default: throw new Exception($"BUG: Resource {resource.ToString()} is a resource but not an Iri or a blank node");
        }
    }


    private IriResource GetApiIriResource(uint resourceId)
    {
        var resource = GetBlankNodeOrIriResource(resourceId);
        if (resource is IriResource r)
            return r;
        throw new ArgumentException($"Resource {resource.ToString()} is not an Iri");
    }

    private GraphElement GetResource(uint resourceId)
    {
        var resource = Triples.GetGraphElement(resourceId);
        if (resource.IsNodeOrEdge)
        {
            var r = resource.resource;
            if (r.IsIri)
                return new IriResource(new IriReference(r.iri));
            if (r.IsAnonymousBlankNode)
                return new BlankNodeResource($"{r.anon_blankNode}");
            throw new Exception("BUG: Resource that is neither Iri nor Blank Node !!");
        }

        if (!resource.IsGraphLiteral) throw new Exception("BUG: Resource that is neither resource or literal!!");
        var lit = resource.literal;
        return new RdfLiteral(lit);
    }


    private Triple EnsureApiTriple(DagSemTools.Rdf.Ingress.Triple triple) =>
        new(GetBlankNodeOrIriResource(triple.subject),
            GetApiIriResource(triple.predicate).Iri,
            GetResource(triple.obj));

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj) =>
        (GetRdfIriGraphElementId(obj, out var objIdx)
         && GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Triples
                .GetTriplesWithObjectPredicate(objIdx, predIdx)
                .Select(EnsureApiTriple)
            : [];


    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate) =>
        (GetRdfIriGraphElementId(subject, out var subjIdx)
         && GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Triples
                .GetTriplesWithSubjectPredicate(subjIdx, predIdx)
                .Select(EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject) =>
        (GetRdfIriGraphElementId(subject, out var subjIdx))
            ? Triples
                .GetTriplesWithSubject(subjIdx)
                .Select(EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate) =>
        (GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Triples
                .GetTriplesWithPredicate(predIdx)
                .Select(EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithObject(IriReference @object) =>
        (GetRdfIriGraphElementId(@object, out var objIdx))
            ? Triples
                .GetTriplesWithObject(objIdx)
                .Select(EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public void EnableOwlReasoning()
    {
        var ontology = new DagSemTools.RdfOwlTranslator.Rdf2Owl(Triples.Triples, Triples.Resources, _logger).extractOntology;
        var ontologyRules = DagSemTools.OWL2RL2Datalog.Library.owl2Datalog(_logger, Triples.Resources, ontology.Ontology);
        LoadDatalog(ontologyRules);
    }
    /// <inheritdoc />
    public void EnableEqualityReasoning() =>
        LoadDatalog(OWL2RL2Datalog.Equality.GetEqualityAxioms(Triples.Resources));


    Datastore IGraph.Datastore => Triples;


}