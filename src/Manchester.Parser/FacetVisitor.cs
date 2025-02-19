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

internal class FacetVisitor : ManchesterBaseVisitor<Iri>
{
    public override Iri VisitFacetLength(ManchesterParser.FacetLengthContext context) => 
        Iri.NewFullIri(Namespaces.XsdLength);
    public override Iri VisitFacetMinLength(ManchesterParser.FacetMinLengthContext context) => 
        Iri.NewFullIri(Namespaces.XsdMinLength);
    public override Iri VisitFacetMaxLength(ManchesterParser.FacetMaxLengthContext context) => 
        Iri.NewFullIri(Namespaces.XsdMaxLength);

    public override Iri VisitFacetLangRange(ManchesterParser.FacetLangRangeContext context) =>
        Iri.NewFullIri(Namespaces.XsdLangRange);
    public override Iri VisitFacetPattern(ManchesterParser.FacetPatternContext context) => 
        Iri.NewFullIri(Namespaces.XsdPattern);
    public override Iri VisitFacetGreaterThan(ManchesterParser.FacetGreaterThanContext context) => 
        Iri.NewFullIri(Namespaces.XsdMinExclusive);
    public override Iri VisitFacetLessThan(ManchesterParser.FacetLessThanContext context) => 
        Iri.NewFullIri(Namespaces.XsdMaxExclusive);
    public override Iri VisitFacetGreaterThanEqual(ManchesterParser.FacetGreaterThanEqualContext context) => 
        Iri.NewFullIri(Namespaces.XsdMinInclusive);
    public override Iri VisitFacetLessThanEqual(ManchesterParser.FacetLessThanEqualContext context) => 
        Iri.NewFullIri(Namespaces.XsdMaxInclusive);

}