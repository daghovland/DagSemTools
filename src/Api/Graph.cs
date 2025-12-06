/*
    Copyright (C) 2024 Dag Hovland
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
        apiTriple.TryGetRdfTriple(apiTriple, out var rdfTriple)
         && Triples
             .ContainsTriple(rdfTriple);

    internal bool GetRdfIriGraphElementId(IriReference subject, out uint subjIdx) =>
        Triples.Resources.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(subject)),
            out subjIdx);

    
    
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


    private static Resource GetBlankNodeOrIriResource(uint resourceId)
    {
        var resource = Triples.GetGraphNode(resourceId);
        if (!FSharpOption<RdfResource>.get_IsSome(resource))
            throw new ArgumentException($"Resource {resource} is not an Iri or a blank node"); ;

        switch (resource.Value)
        {
            case { IsIri: true } r:
                return new IriResource(Triples.Resources, new IriReference(r.iri));
            case { IsAnonymousBlankNode: true } r:
                return new BlankNodeResource($"{r.anon_blankNode}");
            default: throw new Exception($"BUG: Resource {resource.ToString()} is a resource but not an Iri or a blank node");
        }
    }
    
    internal static IriResource GetApiIriResource(uint resourceId)
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
                return new IriResource(Triples.Resources, new IriReference(r.iri));
            if (r.IsAnonymousBlankNode)
                return new BlankNodeResource($"{r.anon_blankNode}");
            throw new Exception("BUG: Resource that is neither Iri nor Blank Node !!");
        }

        if (!resource.IsGraphLiteral) throw new Exception("BUG: Resource that is neither resource or literal!!");
        var lit = resource.literal;
        return new RdfLiteral(Triples.Resources, lit);
    }


    internal Triple EnsureApiTriple(DagSemTools.Rdf.Ingress.Triple triple) =>
        new(Triples.Resources,
            GetBlankNodeOrIriResource(triple.subject),
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