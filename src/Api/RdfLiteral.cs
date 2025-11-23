/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Ingress;
using DagSemTools.Rdf;

namespace DagSemTools.Api;

/// <inheritdoc />
public class RdfLiteral : GraphElement
{
    internal readonly Ingress.RdfLiteral InternalRdfLiteral;
    private readonly GraphElementManager _elementManager;

    internal RdfLiteral(GraphElementManager elementManager, Ingress.RdfLiteral rdfLiteral)
    {
        _elementManager = elementManager;
        InternalRdfLiteral = rdfLiteral;
    }


    /// <summary>
    /// Creates an rdf literal of type xsd:string. This is the default type in rdf
    /// </summary>
    /// <param name="elementManager"></param>
    /// <param name="rdfLiteral"></param>
    /// <returns></returns>
    public static RdfLiteral StringRdfLiteral(GraphElementManager elementManager, string rdfLiteral) =>
        new RdfLiteral(elementManager, DagSemTools.Ingress.RdfLiteral.NewLiteralString(rdfLiteral));

    /// <summary>
    /// Two literals are equal if their string values are equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(GraphElement? other) =>
        other != null && (ReferenceEquals(this, other) ||
                          (other is RdfLiteral literal && literal.InternalRdfLiteral.Equals(InternalRdfLiteral)));

    /// <inheritdoc />
    public override string ToString() => InternalRdfLiteral.ToString();


    /// <inheritdoc />
    public override int GetHashCode() => InternalRdfLiteral.GetHashCode();

    internal override bool GetGraphElementId(out uint idx) =>
        _elementManager.GraphElementMap.TryGetValue(
            DagSemTools.Ingress.GraphElement.NewGraphLiteral(InternalRdfLiteral), out idx);
}
    
    

