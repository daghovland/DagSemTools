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

internal class DataPrimaryVisitor : ManchesterBaseVisitor<DataRange>
{
    DatatypeRestrictionVisitor _datatypeRestrictionVisitor = new DatatypeRestrictionVisitor();
    readonly IVisitorErrorListener _errorListener;
    internal IriGrammarVisitor IriGrammarVisitor { get; init; }
    internal DatatypeVisitor DatatypeVisitor { get; init; }
    public DataPrimaryVisitor(IriGrammarVisitor iriGrammarVisitor, IVisitorErrorListener errorListener)
    {
        IriGrammarVisitor = iriGrammarVisitor;
        DatatypeVisitor = new DatatypeVisitor(iriGrammarVisitor, errorListener);
        _errorListener = errorListener;
    }

    public DataPrimaryVisitor(Dictionary<string, IriReference> prefixes, IVisitorErrorListener errorListener)
    {
        _errorListener = errorListener;
        IriGrammarVisitor = new IriGrammarVisitor(prefixes, _errorListener);
        DatatypeVisitor = new DatatypeVisitor(IriGrammarVisitor, errorListener);
    }
    public override DataRange VisitSingleDataDisjunction(ManchesterParser.SingleDataDisjunctionContext context)
    => Visit(context.dataConjunction());

    public override DataRange VisitSingleDataConjunction(ManchesterParser.SingleDataConjunctionContext context)
        => Visit(context.dataPrimary());
    public override DataRange VisitPositiveDataPrimary(ManchesterParser.PositiveDataPrimaryContext context)
        => Visit(context.dataAtomic());

    public override DataRange VisitDataTypeAtomic(ManchesterParser.DataTypeAtomicContext context)
        => DataRange.NewNamedDataRange(DatatypeVisitor.Visit(context.datatype()));

    public override DataRange VisitDatatypeRestriction(ManchesterParser.DatatypeRestrictionContext context)
    {
        var groundType = DatatypeVisitor.Visit(context.datatype());
        var restrictions = context.datatype_restriction()
            .Select<ManchesterParser.Datatype_restrictionContext, System.Tuple<Iri, GraphElement>>(_datatypeRestrictionVisitor.Visit);
        var fsharp_restriction = ListModule.OfSeq(restrictions);
        return DataRange.NewDatatypeRestriction(groundType, fsharp_restriction);
    }
}