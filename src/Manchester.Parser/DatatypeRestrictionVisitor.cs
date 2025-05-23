/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/


using DagSemTools.Ingress;
using DagSemTools.OwlOntology;

namespace DagSemTools.Manchester.Parser;

internal class DatatypeRestrictionVisitor : ManchesterBaseVisitor<System.Tuple<Iri, GraphElement>>
{
    private FacetVisitor _facetVisitor = new FacetVisitor();

    public override Tuple<Iri, GraphElement> VisitDatatype_restriction(
        ManchesterParser.Datatype_restrictionContext context)
    {
        var (facet, literalTranslator) = _facetVisitor.Visit(context.facet());
        return new(facet, literalTranslator(context.literal()));
    }

}