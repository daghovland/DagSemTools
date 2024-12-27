using DagSemTools.Datalog;
using IriTools;
using DagSemTools.Rdf;
using Microsoft.FSharp.Collections;
using DagSemTools.Ingress;
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
            default: throw new Exception($"BUG: Resource {resource.ToString()} is a resource but not an Iri or a blank node"); ;
        }
    }


    private IriResource GetIriResource(uint resourceId)
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
        if (lit.IsLangLiteral)
            return new RdfLiteral(lit.langliteral);
        if (lit.IsDateLiteral)
            return new RdfLiteral(lit.literalDate.ToString());
        if (lit.IsLiteralString)
            return new RdfLiteral(lit.literal);
        throw new NotImplementedException($"Literal type {lit.ToString()} not implemented. Sorry");
    }

    private Triple GetTriple(DagSemTools.Rdf.Ingress.Triple triple) =>
    new Triple(this.GetBlankNodeOrIriResource(triple.subject),
            GetIriResource(triple.predicate).Iri,
            GetResource(triple.obj));

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj) =>
        (Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(obj)), out var objIdx)
         && Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(predicate)), out var predIdx))
            ? Triples
                .GetTriplesWithObjectPredicate(objIdx, predIdx)
                .Select(GetTriple)
            : [];


    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate) =>
        (Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(subject)), out var subjIdx)
                                                       && Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(predicate)), out var predIdx))
            ? Triples
                .GetTriplesWithSubjectPredicate(subjIdx, predIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject) =>
        (Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(subject)), out var subjIdx))
            ? Triples
                .GetTriplesWithSubject(subjIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate) =>
        (Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(predicate)), out var predIdx))
            ? Triples
                .GetTriplesWithPredicate(predIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithObject(IriReference @object) =>
        (Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(@object)), out var objIdx))
            ? Triples
                .GetTriplesWithObject(objIdx)
                .Select(GetTriple)
            : [];

    /// <inheritdoc />
    public void EnableOwlReasoning()
    {
        var ontology = new DagSemTools.RdfOwlTranslator.Rdf2Owl(Triples.Triples, Triples.Resources).extractOntology;
        var ontologyRules = DagSemTools.OWL2RL2Datalog.Library.owl2Datalog(_logger, Triples.Resources, ontology);
        _rules = _rules.Concat(ontologyRules);
        Reasoner.evaluate(ListModule.OfSeq(_rules), Triples);
    }

    Datastore IGraph.Datastore => Triples;

}