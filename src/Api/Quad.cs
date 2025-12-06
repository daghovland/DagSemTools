/*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using IriTools;
using DagSemTools.Rdf;

namespace DagSemTools.Api;

/// <summary>
/// Represents a quad (a triple with a graphname) in RDF. https://www.w3.org/TR/rdf12-concepts/#dfn-rdf-triple
/// </summary>
public class Quad
{
    private readonly Triple _triple;
    private readonly GraphElementManager _elementManager;

    /// <summary>
    /// The most generic quad constructor. 
    /// </summary>
    /// <param name="elementManager"></param>
    /// <param name="graphName"></param>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <param name="object"></param>
    internal Quad(GraphElementManager elementManager, IriResource graphName, Resource subject, IriReference predicate, GraphElement @object)
    {
        _elementManager = elementManager;
        _triple = new Triple(elementManager, subject, predicate, @object);
        GraphName = graphName;
    }

    /// <summary>
    /// Creates a triple with IRIs on all three places
    /// </summary>
    internal Quad(GraphElementManager elementManager, IriResource graphName, Triple triple)
    {
        _elementManager = elementManager;
        this._triple = triple;
        GraphName = graphName;
    }

    /// <summary>
    /// The subject of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-subject
    /// </summary>
    public Resource Subject  => _triple.Subject; 

    /// <summary>
    /// The predicate of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-predicate
    /// </summary>
    public IriReference Predicate => _triple.Predicate;

    /// <summary>
    /// The object of the triple. https://www.w3.org/TR/rdf12-concepts/#dfn-object
    /// </summary>
    public GraphElement Object => _triple.Object;
    
    /// <summary>
    /// The graph name of the quad. https://www.w3.org/TR/rdf11-datasets/#dfn-rdf-quad
    /// </summary>
    public IriResource GraphName { get; }
}

