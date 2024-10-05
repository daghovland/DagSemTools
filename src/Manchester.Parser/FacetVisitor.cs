/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.AlcTableau;

namespace DagSemTools.Manchester.Parser;

public class FacetVisitor : ManchesterBaseVisitor<DataRange.facet>
{
    public override DataRange.facet VisitFacetLength(ManchesterParser.FacetLengthContext context) => DataRange.facet.Length;
    public override DataRange.facet VisitFacetMinLength(ManchesterParser.FacetMinLengthContext context) => DataRange.facet.MinLength;
    public override DataRange.facet VisitFacetMaxLength(ManchesterParser.FacetMaxLengthContext context) => DataRange.facet.MaxLength;

    public override DataRange.facet VisitFacetLangRange(ManchesterParser.FacetLangRangeContext context) =>
        DataRange.facet.LangRange;
    public override DataRange.facet VisitFacetPattern(ManchesterParser.FacetPatternContext context) => DataRange.facet.Pattern;
    public override DataRange.facet VisitFacetGreaterThan(ManchesterParser.FacetGreaterThanContext context) => DataRange.facet.GreaterThan;
    public override DataRange.facet VisitFacetLessThan(ManchesterParser.FacetLessThanContext context) => DataRange.facet.LessThan;
    public override DataRange.facet VisitFacetGreaterThanEqual(ManchesterParser.FacetGreaterThanEqualContext context) => DataRange.facet.GreaterThanOrEqual;
    public override DataRange.facet VisitFacetLessThanEqual(ManchesterParser.FacetLessThanEqualContext context) => DataRange.facet.LessThanOrEqual;

}