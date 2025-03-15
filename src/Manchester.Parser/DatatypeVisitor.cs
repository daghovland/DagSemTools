/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Ingress;
using DagSemTools.Parser;
using IriTools;
using DagSemTools.Manchester.Parser;
using DagSemTools.OwlOntology;
using Microsoft.FSharp.Collections;

namespace DagSemTools.ManchesterAntlr;

internal class DatatypeVisitor : ManchesterBaseVisitor<Iri>
{
    DatatypeRestrictionVisitor _datatypeRestrictionVisitor = new DatatypeRestrictionVisitor();
    readonly IVisitorErrorListener _errorListener;
    internal IriGrammarVisitor IriGrammarVisitor { get; init; }

    public DatatypeVisitor(IriGrammarVisitor iriGrammarVisitor, IVisitorErrorListener errorListener)
    {
        IriGrammarVisitor = iriGrammarVisitor;
        _errorListener = errorListener;
    }

    public DatatypeVisitor(Dictionary<string, IriReference> prefixes, IVisitorErrorListener errorListener)
    {
        _errorListener = errorListener;
        IriGrammarVisitor = new IriGrammarVisitor(prefixes, _errorListener);
    }
    public override Iri VisitSingleDataDisjunction(ManchesterParser.SingleDataDisjunctionContext context)
    => Visit(context.dataConjunction());

    public override Iri VisitSingleDataConjunction(ManchesterParser.SingleDataConjunctionContext context)
        => Visit(context.dataPrimary());

    public override Iri VisitPositiveDataPrimary(ManchesterParser.PositiveDataPrimaryContext context)
        => Visit(context.dataAtomic());


    public override Iri VisitDataTypeAtomic(ManchesterParser.DataTypeAtomicContext context)
        => Visit(context.datatype());

    public override Iri VisitDatatypeInteger(ManchesterParser.DatatypeIntegerContext context)
        => Iri.NewFullIri(Namespaces.XsdInteger);

    public override Iri VisitDatatypeString(ManchesterParser.DatatypeStringContext context)
        => Iri.NewFullIri(Namespaces.XsdString);

    public override Iri VisitDatatypeDecimal(ManchesterParser.DatatypeDecimalContext context)
        => Iri.NewFullIri(Namespaces.XsdDecimal);

    public override Iri VisitDatatypeFloat(ManchesterParser.DatatypeFloatContext context)
        => Iri.NewFullIri(Namespaces.XsdFloat);

    public override Iri VisitDatatypeIri(ManchesterParser.DatatypeIriContext context)
        => Iri.NewFullIri(IriGrammarVisitor.Visit(context.rdfiri()));


}