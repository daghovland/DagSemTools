/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Ingress;
using IriTools;
using DagSemTools.Rdf;

namespace DagSemTools.Api;

/// <summary>
/// Represents a triple in RDF. https://www.w3.org/TR/rdf12-concepts/#dfn-rdf-triple
/// </summary>
public class Triple
{
    private GraphElementManager _elementManager;

    /// <summary>
    /// The most generic triple constructor. 
    /// </summary>
    /// <param name="elementManager"></param>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <param name="object"></param>
    internal Triple(GraphElementManager elementManager, Resource subject, IriReference predicate, GraphElement @object)
    {
        _elementManager = elementManager;
        Subject = subject;
        Predicate = predicate;
        Object = @object;
    }

    /// <summary>
    /// Creates a triple with IRIs on all three places
    /// </summary>
    /// <param name="elementManager"></param>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <param name="object"></param>
    internal Triple(GraphElementManager elementManager, IriReference subject, IriReference predicate, IriReference @object)
    {
        _elementManager = elementManager;
        Subject = new IriResource( elementManager, subject);
        Predicate = predicate;
        Object = new IriResource(elementManager, @object);
    }

    /// <summary>
    /// The subject of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-subject
    /// </summary>
    public Resource Subject { get; }

    /// <summary>
    /// The predicate of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-predicate
    /// </summary>
    public IriReference Predicate { get; }

    /// <summary>
    /// The object of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-object
    /// </summary>
    public GraphElement Object { get; }
    
    internal bool TryGetRdfTriple(Triple apiTriple, out Rdf.Ingress.Triple rdfTriple)
    {
        if (apiTriple.Subject.GetGraphElementId(out var subjIdx) &&
            apiTriple.Object.GetGraphElementId( out var objIdx))
        {
            var predIdx = _elementManager.AddNodeResource(RdfResource.NewIri(apiTriple.Predicate));
            rdfTriple = new Rdf.Ingress.Triple(subjIdx, predIdx, objIdx);
            return true;
        }

        rdfTriple = default;
        return false;
    }
    internal Rdf.Ingress.Triple EnsureRdfTriple(Triple apiTriple) =>
        (apiTriple.Subject.GetGraphElementId(out var subjIdx) &&
         _elementManager.GraphElementMap.TryGetValue(Ingress.GraphElement.NewNodeOrEdge(RdfResource.NewIri(apiTriple.Predicate)), out var predIdx) &&
         apiTriple.Object.GetGraphElementId(out var objIdx)) ?
            new Rdf.Ingress.Triple(subjIdx, predIdx, objIdx) :
            throw new Exception($"BUG: Something went wrong when translating {apiTriple}");



}

