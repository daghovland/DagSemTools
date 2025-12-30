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
    internal Graph(TripleTable triples, GraphElementManager _elementManager, ILogger? logger = null)
    {
        Triples = triples;
        ElementManager = _elementManager;
        Resources = new ResourceManager(ElementManager);
        _logger = logger ?? new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }
    private ResourceManager Resources { get; init; }
    private GraphElementManager ElementManager { get; init; }
    private TripleTable Triples { get; init; }

    private IEnumerable<Rule> _rules = Enumerable.Empty<Rule>();

    /// <inheritdoc />
    public bool ContainsTriple(Triple apiTriple) =>
        apiTriple.TryGetRdfTriple(apiTriple, out var rdfTriple)
         && Triples
             .Contains(rdfTriple);

    internal bool GetRdfIriGraphElementId(IriReference subject, out uint subjIdx) =>
        ElementManager.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(subject)),
            out subjIdx);




    /// <inheritdoc />
    public bool IsEmpty() => Triples.TripleCount == 0;


    private Resource GetBlankNodeOrIriResource(uint resourceId)
    {
        var resource = ElementManager.GetGraphNode(resourceId);
        if (!FSharpOption<RdfResource>.get_IsSome(resource))
            throw new ArgumentException($"Resource {resource} is not an Iri or a blank node"); ;

        switch (resource.Value)
        {
            case { IsIri: true } r:
                return new IriResource(ElementManager, new IriReference(r.iri));
            case { IsAnonymousBlankNode: true } r:
                return new BlankNodeResource($"{r.anon_blankNode}");
            default: throw new Exception($"BUG: Resource {resource.ToString()} is a resource but not an Iri or a blank node");
        }
    }

    internal IriResource GetApiIriResource(uint resourceId)
    {
        var resource = GetBlankNodeOrIriResource(resourceId);
        if (resource is IriResource r)
            return r;
        throw new ArgumentException($"Resource {resource.ToString()} is not an Iri");
    }
    /// <summary>
    /// Factory method for creating an owl ontology.
    /// </summary>
    /// <returns>An owl ontology object, which can be used for RL reasoning</returns>
    public OwlOntology ParseToOntology() =>
        new(Triples, ElementManager, _logger);

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj) =>
        (GetRdfIriGraphElementId(obj, out var objIdx)
         && GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Triples
                .GetTriplesWithObjectPredicate(objIdx, predIdx)
                .Select(Resources.EnsureApiTriple)
            : [];


    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate) =>
        (GetRdfIriGraphElementId(subject, out var subjIdx)
         && GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Triples
                .GetTriplesWithSubjectPredicate(subjIdx, predIdx)
                .Select(Resources.EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject) =>
        (GetRdfIriGraphElementId(subject, out var subjIdx))
            ? Triples
                .GetTriplesWithSubject(subjIdx)
                .Select(Resources.EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate) =>
        (GetRdfIriGraphElementId(predicate, out var predIdx))
            ? Triples
                .GetTriplesWithPredicate(predIdx)
                .Select(Resources.EnsureApiTriple)
            : [];

    /// <inheritdoc />
    public IEnumerable<Triple> GetTriplesWithObject(IriReference @object) =>
        (GetRdfIriGraphElementId(@object, out var objIdx))
            ? Triples
                .GetTriplesWithObject(objIdx)
                .Select(Resources.EnsureApiTriple)
            : [];




}