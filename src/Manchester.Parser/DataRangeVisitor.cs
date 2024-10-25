/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.AlcTableau;
using DagSemTools.Parser;
using IriTools;
using DagSemTools.Manchester.Parser;
using Microsoft.FSharp.Collections;

namespace DagSemTools.ManchesterAntlr;

internal class DataRangeVisitor : ManchesterBaseVisitor<DataRange.Datarange>
{
    DatatypeRestrictionVisitor _datatypeRestrictionVisitor = new DatatypeRestrictionVisitor();
    readonly IVisitorErrorListener _errorListener;
    internal IriGrammarVisitor IriGrammarVisitor { get; init; }
    public DataRangeVisitor(IVisitorErrorListener errorListener)
    {
        IriGrammarVisitor = new IriGrammarVisitor(errorListener);
        _errorListener = errorListener;
    }
    public DataRangeVisitor(IriGrammarVisitor iriGrammarVisitor, IVisitorErrorListener errorListener)
    {
        IriGrammarVisitor = iriGrammarVisitor;
        _errorListener = errorListener;
    }

    public DataRangeVisitor(Dictionary<string, IriReference> prefixes, IVisitorErrorListener errorListener)
    {
        _errorListener = errorListener;
        IriGrammarVisitor = new IriGrammarVisitor(prefixes, _errorListener);
    }
    public override DataRange.Datarange VisitSingleDataDisjunction(ManchesterParser.SingleDataDisjunctionContext context)
    => Visit(context.dataConjunction());

    public override DataRange.Datarange VisitSingleDataConjunction(ManchesterParser.SingleDataConjunctionContext context)
        => Visit(context.dataPrimary());

    public override DataRange.Datarange VisitPositiveDataPrimary(ManchesterParser.PositiveDataPrimaryContext context)
        => Visit(context.dataAtomic());


    public override DataRange.Datarange VisitDataTypeAtomic(ManchesterParser.DataTypeAtomicContext context)
        => Visit(context.datatype());

    public override DataRange.Datarange VisitDatatypeInteger(ManchesterParser.DatatypeIntegerContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#integer");

    public override DataRange.Datarange VisitDatatypeString(ManchesterParser.DatatypeStringContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#string");

    public override DataRange.Datarange VisitDatatypeDecimal(ManchesterParser.DatatypeDecimalContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#decimal");

    public override DataRange.Datarange VisitDatatypeFloat(ManchesterParser.DatatypeFloatContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#float");

    public override DataRange.Datarange VisitDatatypeIri(ManchesterParser.DatatypeIriContext context)
        => DataRange.Datarange.NewDatatype(IriGrammarVisitor.Visit(context.rdfiri()));

    public override DataRange.Datarange VisitDatatypeRestriction(ManchesterParser.DatatypeRestrictionContext context)
    {
        var groundType = Visit(context.datatype());
        var restrictions = context.datatype_restriction()
            .Select<ManchesterParser.Datatype_restrictionContext, System.Tuple<DataRange.facet, string>>(_datatypeRestrictionVisitor.Visit);
        var fsharp_restriction = ListModule.OfSeq(restrictions);
        return DataRange.Datarange.NewRestriction(groundType, fsharp_restriction);
    }
}