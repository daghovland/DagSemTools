/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using IriTools;
using Microsoft.FSharp.Core;
using DagSemTools.Rdf;
using DagSemTools.Parser;

namespace DagSemTools.Turtle.Parser;

internal class TurtleListener : TurtleDocBaseListener
{

    private IriGrammarVisitor _iriGrammarVisitor;
    private ResourceVisitor _resourceVisitor;
    private FSharpOption<IriReference> _graphName;
    private readonly IVisitorErrorListener _errorListener;
    public Datastore datastore { get; init; }

    public TurtleListener(uint initSize, IVisitorErrorListener errorListener)
    {
        datastore = new Datastore(initSize);
        _graphName = FSharpOption<IriReference>.None;
        _iriGrammarVisitor = new IriGrammarVisitor(DefaultPrefixes());
        _resourceVisitor = new ResourceVisitor(datastore, _iriGrammarVisitor);
        _errorListener = errorListener;
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
    public override void ExitBaseDeclaration(TurtleDocParser.BaseDeclarationContext context)
    {
        var iriString = GetStringExcludingFirstAndLast(context.ABSOLUTEIRIREF().GetText());
        var iri = new IriReference(iriString);
        _iriGrammarVisitor.SetBase(iri);
    }

    public override void ExitPrefixId(TurtleDocParser.PrefixIdContext context)
    {
        var prefix = GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }
    public override void ExitSparqlPrefix(TurtleDocParser.SparqlPrefixContext context)
    {
        var prefix = GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }

    public override void ExitNamedSubjectTriples(TurtleDocParser.NamedSubjectTriplesContext context)
    {
        var curSubject = _resourceVisitor.Visit(context.subject());
        var triples = _resourceVisitor._predicateObjectListVisitor.Visit(context.predicateObjectList())(curSubject);
        triples.ToList().ForEach(triple => datastore.AddTriple(triple));
    }


    public override void ExitBlankNodeTriples(TurtleDocParser.BlankNodeTriplesContext context)
    {
        var blankNode = datastore.NewAnonymousBlankNode();
        var internalTriples =
            _resourceVisitor._predicateObjectListVisitor.Visit(
                context.blankNodePropertyList().predicateObjectList())(blankNode);
        var postTriples = context.predicateObjectList() switch
        {
            null => new List<Ingress.Triple>(),
            var c => _resourceVisitor._predicateObjectListVisitor.Visit(c)(blankNode)
        };
        var triples = internalTriples.Concat(postTriples);
        triples.ToList().ForEach(triple => datastore.AddTriple(triple));
    }
}

