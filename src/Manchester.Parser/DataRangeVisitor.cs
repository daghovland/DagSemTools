/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Parser;
using IriTools;
using DagSemTools.Manchester.Parser;
using DagSemTools.OwlOntology;
using Microsoft.FSharp.Collections;

namespace DagSemTools.ManchesterAntlr;

internal class DataRangeVisitor : ManchesterBaseVisitor<DataRange>
{
    DatatypeRestrictionVisitor _datatypeRestrictionVisitor = new DatatypeRestrictionVisitor();
    readonly IVisitorErrorListener _errorListener;
    internal IriGrammarVisitor IriGrammarVisitor { get; init; }
    internal DatatypeVisitor DatatypeVisitor { get; init; }
    public DataRangeVisitor(IVisitorErrorListener errorListener)
    {
        IriGrammarVisitor = new IriGrammarVisitor(errorListener);
        DatatypeVisitor = new DatatypeVisitor(IriGrammarVisitor, errorListener);
        _errorListener = errorListener;
    }
    public DataRangeVisitor(IriGrammarVisitor iriGrammarVisitor, IVisitorErrorListener errorListener)
    {
        IriGrammarVisitor = iriGrammarVisitor;
        DatatypeVisitor = new DatatypeVisitor(IriGrammarVisitor, errorListener);
        _errorListener = errorListener;
    }

    public DataRangeVisitor(Dictionary<string, IriReference> prefixes, IVisitorErrorListener errorListener)
    {
        _errorListener = errorListener;
        IriGrammarVisitor = new IriGrammarVisitor(prefixes, _errorListener);
        DatatypeVisitor = new DatatypeVisitor(IriGrammarVisitor, errorListener);
    }
    public override DataRange VisitSingleDataDisjunction(ManchesterParser.SingleDataDisjunctionContext context)
    => Visit(context.dataConjunction());

    public override DataRange VisitSingleDataConjunction(ManchesterParser.SingleDataConjunctionContext context)
        => Visit(context.dataPrimary());


}