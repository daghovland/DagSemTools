/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using System.Globalization;
using DagSemTools;
using DagSemTools.Parser;
using Microsoft.FSharp.Collections;
using DagSemTools.Rdf;
using DagSemTools.Datalog.Parser;
using IriTools;

namespace DagSemTools.Datalog.Parser;

internal class DatalogListener : DatalogBaseListener
{
    private readonly IVisitorErrorListener _errorListener;

    private readonly IriGrammarVisitor _iriGrammarVisitor;
    private readonly RuleAtomVisitor _ruleAtomVisitor;
    public IEnumerable<Datalog.Rule> DatalogProgram { get; private set; }

    internal DatalogListener(Datastore datastore, IVisitorErrorListener errorListener)
    {
        _errorListener = errorListener;
        DatalogProgram = new List<Datalog.Rule>();
        _iriGrammarVisitor = new IriGrammarVisitor(DefaultPrefixes());
        var resourceVisitor = new ResourceVisitor(datastore, _iriGrammarVisitor);
        var predicateVisitor = new PredicateVisitor(resourceVisitor);
        _ruleAtomVisitor = new RuleAtomVisitor(predicateVisitor);
    }


    private static Dictionary<string, IriReference> DefaultPrefixes()
    {
        var prefixes = new Dictionary<string, IriReference>();
        prefixes.TryAdd("rdf", new IriReference("https://www.w3.org/1999/02/22-rdf-syntax-ns#"));
        prefixes.TryAdd("rdfs", new IriReference("https://www.w3.org/2000/01/rdf-schema#"));
        prefixes.TryAdd("xsd", new IriReference("https://www.w3.org/2001/XMLSchema#"));
        prefixes.TryAdd("owl", new IriReference("https://www.w3.org/2002/07/owl#"));
        return prefixes;
    }

    /// <summary>
    /// Used to transform input into iri in the handlers below
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string GetStringExcludingFirstAndLast(string input)
    {
        if (input.Length > 2)
        {
            return input.Substring(1, input.Length - 2);
        }
        return string.Empty;
    }

    /// <summary>
    /// Used to transform prefix: into prefix in the methods below
    /// </summary>
    /// <param name="prefixNs"></param>
    public static string GetStringExcludingLastColon(string prefixNs)
    {
        if (prefixNs.Length >= 1 && prefixNs[^1] == ':')
        {
            return prefixNs.Substring(0, prefixNs.Length - 1);
        }
        throw new Exception($"Invalid prefix {prefixNs}. Prefix should end with ':'");
    }
    public override void ExitBaseDeclaration(DatalogParser.BaseDeclarationContext context)
    {
        var iriString = GetStringExcludingFirstAndLast(context.ABSOLUTEIRIREF().GetText());
        var iri = new IriReference(iriString);
        _iriGrammarVisitor.SetBase(iri);
    }

    public override void ExitPrefixId(DatalogParser.PrefixIdContext context)
    {
        var prefix = GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }
    public override void ExitSparqlPrefix(DatalogParser.SparqlPrefixContext context)
    {
        var prefix = GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }

    public override void ExitRule(DatalogParser.RuleContext context)
    {
        var headCtxt = context.head();
        var headAtom = _ruleAtomVisitor.TriplePatternVisitor.Visit(headCtxt);

        var body =
            context.body().ruleAtom()
                .Select(b => _ruleAtomVisitor.Visit(b));
        DatalogProgram = DatalogProgram.Append(new Datalog.Rule(headAtom, ListModule.OfSeq(body)));
    }

}