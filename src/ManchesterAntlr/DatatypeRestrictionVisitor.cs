/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using AlcTableau;

namespace ManchesterAntlr;

public class DatatypeRestrictionVisitor : ManchesterBaseVisitor<System.Tuple<DataRange.facet, string>>
{
    private FacetVisitor _facetVisitor = new FacetVisitor();
    public override Tuple<DataRange.facet, string> VisitDatatype_restriction(ManchesterParser.Datatype_restrictionContext context)
        => new(_facetVisitor.Visit(context.facet()), context.literal().GetText());

}