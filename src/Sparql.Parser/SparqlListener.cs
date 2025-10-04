/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using Antlr4.Runtime;
using DagSemTools.Parser;
using DagSemTools.Rdf;
using IriTools;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Sparql.Parser;

/// <summary>
/// ANTLR listener that builds a SelectQuery from a SPARQL parse tree.
/// </summary>
internal class SparqlListener : SparqlBaseListener
{
    private readonly IVisitorErrorListener _errorListener;
    private IriGrammarVisitor _iriGrammarVisitor;

    private IriReference? _baseIriReference;
    private readonly Dictionary<string, IriReference> _prefixes;
    private Query.SelectQuery? _result = null;

    public SparqlListener(IVisitorErrorListener errorListener) :
        this(errorListener, new GraphElementManager(100)){
    }

    public SparqlListener(IVisitorErrorListener errorListener, Dictionary<string, IriReference> prefixes)
        : this(errorListener, prefixes, new GraphElementManager(100)){
    }
    public SparqlListener(IVisitorErrorListener errorListener, GraphElementManager elementManager)
    : this(errorListener, new Dictionary<string, IriReference>(), elementManager){
    }
    public SparqlListener(IVisitorErrorListener errorListener, Dictionary<string, IriReference> prefixes, GraphElementManager elementManager)
    {
        ElementManager = elementManager;
        _errorListener = errorListener;
        _iriGrammarVisitor = new IriGrammarVisitor(DefaultPrefixes());
        _prefixes = prefixes;
    }
    
    private static Dictionary<string, IriReference> DefaultPrefixes()
    {
        var prefixes = new Dictionary<string, IriReference>();
        prefixes.TryAdd("rdf", new IriReference("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
        prefixes.TryAdd("rdfs", new IriReference("http://www.w3.org/2000/01/rdf-schema#"));
        prefixes.TryAdd("xsd", new IriReference("http://www.w3.org/2001/XMLSchema#"));
        prefixes.TryAdd("owl", new IriReference("http://www.w3.org/2002/07/owl#"));
        return prefixes;
    }
    
    /// <summary>
    /// The result of walking the parse tree.
    /// Populate this in the appropriate exit/enter methods.
    /// </summary>
    public Query.SelectQuery Result =>  this._result ?? throw new InvalidOperationException("Parser result does not exist before parsing has been done");

    public GraphElementManager ElementManager { get; }
    
    public override void EnterBaseDecl(SparqlParser.BaseDeclContext context)
    {
        var iriToken = context.IRIREF()?.GetText();
        if (string.IsNullOrWhiteSpace(iriToken))
            return;

        try
        {
            _baseIriReference = new IriReference(DagSemTools.Parser.ParserUtils.TrimIri(iriToken));
        }
        catch (Exception ex)
        {
            Report(context, $"Invalid BASE IRI: {ex.Message}");
        }
    }

    public override void EnterPrefixDecl(SparqlParser.PrefixDeclContext context)
    {
        var nsToken = context.PNAME_NS()?.GetText();
        var iriToken = context.IRIREF()?.GetText();
        if (string.IsNullOrWhiteSpace(nsToken) || string.IsNullOrWhiteSpace(iriToken))
            return;

        // PNAME_NS includes the trailing ':', strip it
        var prefix = nsToken.EndsWith(":") ? nsToken[..^1] : nsToken;

        try
        {
            _prefixes[prefix] = new IriReference(ParserUtils.TrimIri(iriToken));
        }
        catch (Exception ex)
        {
            Report(context, $"Invalid PREFIX IRI for '{prefix}': {ex.Message}");
        }
    }

    // TODO Handle distinct, reduced, * etc.

    public override void ExitSelectQuery(SparqlParser.SelectQueryContext context)
    {
        var projection = context.selectClause();
        var parsedVars = projection.projection().
            Select(v => new ProjectionVisitor().Visit(v))
            .ToList();
        var whereClause = context.whereClause();
        var solutionModifier = context.solutionModifier();
        _result = new Query.SelectQuery(ListModule.OfSeq(parsedVars), FSharpList<Query.TriplePattern>.Empty);
    }


    private void Report(ParserRuleContext ctx, string message)
    {
        var token = ctx.Start ?? throw new Exception($"Lacking token for parsing error {message}");
        var line = token.Line;
        var col = token.Column;
         _errorListener.VisitorError(token , line, col, message);
    }
}