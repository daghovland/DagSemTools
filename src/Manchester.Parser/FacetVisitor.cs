/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Ingress;
using DagSemTools.OwlOntology;
using DagSemTools.Manchester.Parser;

namespace DagSemTools.Manchester.Parser;

internal class FacetVisitor : ManchesterBaseVisitor<(Iri, Func<ManchesterParser.LiteralContext, GraphElement>)>
{
    internal static GraphElement IntegerLiteral(ManchesterParser.LiteralContext ctxt)
    => GraphElement.NewGraphLiteral(RdfLiteral.NewIntegerLiteral(Int32.Parse(ctxt.children.First().GetText())));

    internal static GraphElement StringLiteral(ManchesterParser.LiteralContext ctxt)
        => GraphElement.NewGraphLiteral(RdfLiteral.NewLiteralString(ctxt.children.First().GetText()));
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetLength(ManchesterParser.FacetLengthContext context) =>
        (Iri.NewFullIri(Namespaces.XsdLength), IntegerLiteral);
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetMinLength(ManchesterParser.FacetMinLengthContext context) =>
        (Iri.NewFullIri(Namespaces.XsdMinLength), IntegerLiteral);
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetMaxLength(ManchesterParser.FacetMaxLengthContext context) =>
        (Iri.NewFullIri(Namespaces.XsdMaxLength), IntegerLiteral);

    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetLangRange(ManchesterParser.FacetLangRangeContext context) =>
        (Iri.NewFullIri(Namespaces.XsdLangRange), StringLiteral);
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetPattern(ManchesterParser.FacetPatternContext context) =>
        (Iri.NewFullIri(Namespaces.XsdPattern), StringLiteral);
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetGreaterThan(ManchesterParser.FacetGreaterThanContext context) =>
        (Iri.NewFullIri(Namespaces.XsdMinExclusive), IntegerLiteral);
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetLessThan(ManchesterParser.FacetLessThanContext context) =>
        (Iri.NewFullIri(Namespaces.XsdMaxExclusive), IntegerLiteral);
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetGreaterThanEqual(ManchesterParser.FacetGreaterThanEqualContext context) =>
        (Iri.NewFullIri(Namespaces.XsdMinInclusive), IntegerLiteral);
    public override (Iri, Func<ManchesterParser.LiteralContext, GraphElement>) VisitFacetLessThanEqual(ManchesterParser.FacetLessThanEqualContext context) =>
        (Iri.NewFullIri(Namespaces.XsdMaxInclusive), IntegerLiteral);

}