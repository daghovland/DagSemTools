/*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

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
/// Implementation of an rdf dataset. 
/// </summary>
public class Dataset : IDataset
{
    private ILogger _logger;
    internal Dataset(Datastore quads, ILogger? logger = null)
    {
        Quads = quads;
        Resources = new(quads.Resources);
        _logger = logger ?? new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        DefaultGraph = new Graph(quads.Triples, quads.Resources, logger);
    }

    private Datastore Quads { get; init; }
    private ResourceManager Resources { get; init; }
    ResourceManager IDataset.GetResourceManager() => Resources;
    private IGraph DefaultGraph { get; init; }

    
    /// <inheritdoc />
    public IGraph GetNamedGraph(IriReference graphName)
    {
        var graphNameResource = Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(graphName));
        var graphId = Quads.Resources.GraphElementMap[graphNameResource];
        var namedGraph = Quads.NamedGraphs.GetGraph(graphId);
        return new Graph(namedGraph, Quads.Resources, _logger);
    }
    /// <inheritdoc />
    public IGraph GetDefaultGraph() => DefaultGraph;
    
    private IEnumerable<Rule> _rules = Enumerable.Empty<Rule>();

    /// Checks whether the default graph contains the given triple.
    public bool ContainsTriple(Triple apiTriple) => DefaultGraph.ContainsTriple(apiTriple);

    /// <summary>
    /// Checks whether the quad exists in the dataset.
    /// </summary>
    /// <param name="quad"></param>
    /// <returns></returns>
    public bool ContainsQuad(Quad quad)
    {
        return (GetRdfResourceGraphElementId(quad.GraphName, out var graphIdx)
                && GetRdfIriGraphElementId(quad.Predicate, out var predIdx)
                && GetRdfResourceGraphElementId(quad.Subject, out var subjIdx)
                && GetRdfGraphElementId(quad.Object, out var objIdx))
            && Quads.ContainsQuad(new Rdf.Ingress.Quad(graphIdx, subjIdx, predIdx, objIdx));
    }

    private bool GetRdfIriGraphElementId(IriReference subject, out uint subjIdx) =>
        Quads.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(subject)),
            out subjIdx);

    private bool GetRdfLiteralGraphElementId(RdfLiteral literal, out uint subjIdx) =>
        Quads.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewGraphLiteral(literal.InternalRdfLiteral), out subjIdx);

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
            Quads ?? throw new InvalidOperationException());
        LoadDatalog(newRules);
    }

    /// <inheritdoc />
    public IEnumerable<Dictionary<string, GraphElement>> AnswerSelectQuery(string query)
    {
        var parsedQuery = Sparql.Parser.Parser.ParseString(query, Console.Error, Quads.Resources);
        var results = QueryProcessor.Answer(Quads, parsedQuery.Item1);
        return results
            .Map(r =>
                r.ToDictionary(kv => kv.Key, kv => Resources.GetResource(kv.Value)))
            .ToList();
    }

    /// <inheritDoc />
    public void LoadDatalog(IEnumerable<Rule> newRules)
    {
        _rules = _rules.Concat(newRules);
        Reasoner.evaluate(_logger, ListModule.OfSeq(_rules), Quads);
    }

    /// <inheritdoc />
    public bool IsEmpty() => Quads.Triples.TripleCount == 0;


    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj) =>
        DefaultGraph.GetTriplesWithPredicateObject(predicate, obj);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Quad> GetQuadsWithPredicateObject(IriReference predicate, IriReference obj) =>

    (GetRdfIriGraphElementId(obj, out var objIdx)
         && GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Quads.NamedGraphs
                .GetQuadsWithObjectPredicate(objIdx, predIdx)
                .Select(Resources.EnsureApiQuad)
            : [];



    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate) =>
        (GetRdfIriGraphElementId(subject, out var subjIdx)
         && GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Quads
                .GetTriplesWithSubjectPredicate(subjIdx, predIdx)
                .Select(Resources.EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject) =>
        (GetRdfIriGraphElementId(subject, out var subjIdx))
            ? Quads
                .GetTriplesWithSubject(subjIdx)
                .Select(Resources.EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate) =>
        (GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Quads
                .GetTriplesWithPredicate(predIdx)
                .Select(Resources.EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithObject(IriReference @object) =>
        (GetRdfIriGraphElementId(@object, out var objIdx))
            ? Quads
                .GetTriplesWithObject(objIdx)
                .Select(Resources.EnsureApiTriple)
            : [];




    /// <inheritdoc />
    public IGraph GetMergedTriples()
    {
        throw new NotImplementedException();
    }


    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference graphName, IriReference predicate, IriReference obj)
    {
        throw new NotImplementedException();
    }


    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference graphName, IriReference subject, IriReference predicate) =>
        (GetRdfIriGraphElementId(subject, out var subjIdx)
         && GetRdfIriGraphElementId(predicate, out var predIdx)
        && GetRdfIriGraphElementId(graphName, out var graphIdx))
            ? Quads.NamedGraphs
                .GetTriplesWithIdSubjectPredicate(graphIdx, subjIdx, predIdx)
                .Select(Resources.EnsureApiTriple)
            : [];
    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference graphName, IriReference subject)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference graphName, IriReference predicate)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithObject(IriReference graphName, IriReference obj)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public bool ContainsTriple(IriReference graphName, Triple triple)
    {
        throw new NotImplementedException();
    }

    // TODO FIND BETTER API DESIGN FOR THIS
    /// <inheritdoc />
    public void EnableOwlReasoning()
    {
        var ontology = new DagSemTools.RdfOwlTranslator.Rdf2Owl(Quads.Triples, Quads.Resources, _logger).extractOntology;
        var ontologyRules = DagSemTools.OWL2RL2Datalog.Library.owl2Datalog(_logger, Quads.Resources, ontology.Ontology);
        LoadDatalog(ontologyRules);
    }
    /// <inheritdoc />
    public void EnableEqualityReasoning() =>
        LoadDatalog(OWL2RL2Datalog.Equality.GetEqualityAxioms(Quads.Resources));



}