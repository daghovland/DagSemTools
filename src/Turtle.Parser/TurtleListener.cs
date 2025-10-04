/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using System.Net.Mime;
using DagSemTools.Ingress;
using IriTools;
using Microsoft.FSharp.Core;
using DagSemTools.Rdf;
using DagSemTools.Parser;

namespace DagSemTools.Turtle.Parser;

internal class TurtleListener : TriGDocBaseListener
{

    private IriGrammarVisitor _iriGrammarVisitor;
    private ResourceVisitor _resourceVisitor;
    internal uint? GraphName;
    private readonly IVisitorErrorListener _errorListener;
    public Datastore datastore { get; init; }

    public TurtleListener(uint initSize, IVisitorErrorListener errorListener)
    {
        datastore = new Datastore(initSize);
        _iriGrammarVisitor = new IriGrammarVisitor(DefaultPrefixes());
        _resourceVisitor = new ResourceVisitor(datastore, _iriGrammarVisitor);
        _errorListener = errorListener;
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

    

    public override void ExitBaseDeclaration(TriGDocParser.BaseDeclarationContext context)
    {
        var iriString = DagSemTools.Parser.ParserUtils.TrimIri(context.ABSOLUTEIRIREF().GetText());
        var iri = new IriReference(iriString);
        _iriGrammarVisitor.SetBase(iri);
    }

    public override void ExitPrefixId(TriGDocParser.PrefixIdContext context)
    {
        var prefix = ParserUtils.GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }
    public override void ExitSparqlPrefix(TriGDocParser.SparqlPrefixContext context)
    {
        var prefix = ParserUtils.GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }

    public override void ExitNamedSubjectTriples(TriGDocParser.NamedSubjectTriplesContext context)
    {
        var curSubject = _resourceVisitor.Visit(context.subject());
        ExitNamedTripleList(curSubject, context.predicateObjectList());
    }

    public override void ExitCollectionTriples2(TriGDocParser.CollectionTriples2Context context)
    {
        var curSubject = _resourceVisitor.Visit(context.collection());
        ExitNamedTripleList(curSubject, context.predicateObjectList());
    }

    public override void ExitLabelOrSubjectTriples(TriGDocParser.LabelOrSubjectTriplesContext context)
    {
        var curSubject = _resourceVisitor.Visit(context.labelOrSubject());
        ExitNamedTripleList(curSubject, context.predicateObjectList());
    }



    public override void ExitReifiedTripleObjectList(TriGDocParser.ReifiedTripleObjectListContext context)
    {
        var curSubject = _resourceVisitor.Visit(context.reifiedTriple());
        ExitNamedTripleList(curSubject, context.predicateObjectList());
    }


    private void ExitNamedTripleList(uint curSubject, TriGDocParser.PredicateObjectListContext predicateObjectListContext)
    {
        var triples = _resourceVisitor._predicateObjectListVisitor.Visit(predicateObjectListContext)(curSubject);
        if (GraphName == null)
            triples.ToList().ForEach(triple => datastore.AddTriple(triple));
        else
            triples.ToList().ForEach(triple => datastore.AddNamedGraphTriple(GraphName.Value, triple));
    }

    public override void ExitBlankNodeTriples2(TriGDocParser.BlankNodeTriples2Context context) =>
        ExitAnyBlankNodeTriples(context.blankNodePropertyList(), context.predicateObjectList());

    public override void ExitBlankNodeTriples(TriGDocParser.BlankNodeTriplesContext context) =>
        ExitAnyBlankNodeTriples(context.blankNodePropertyList(), context.predicateObjectList());

    private void ExitAnyBlankNodeTriples(TriGDocParser.BlankNodePropertyListContext blankNodePropertyList, TriGDocParser.PredicateObjectListContext predicateObjectList)
    {
        var blankNode = datastore.NewAnonymousBlankNode();
        var internalTriples =
            _resourceVisitor._predicateObjectListVisitor.Visit(
                blankNodePropertyList.predicateObjectList())(blankNode);
        var postTriples = predicateObjectList switch
        {
            null => new List<Rdf.Ingress.Triple>(),
            var c => _resourceVisitor._predicateObjectListVisitor.Visit(c)(blankNode)
        };
        var triples = internalTriples.Concat(postTriples);
        if (GraphName == null)
            triples.ToList().ForEach(triple => datastore.AddTriple(triple));
        else
            triples.ToList().ForEach(triple => datastore.AddNamedGraphTriple(GraphName.Value, triple));
    }

    public override void EnterBlockNamedWrappedGraph(TriGDocParser.BlockNamedWrappedGraphContext context) =>
        GraphName = _resourceVisitor.Visit(context.labelOrSubject());
    public override void ExitBlockNamedWrappedGraph(TriGDocParser.BlockNamedWrappedGraphContext context) =>
        GraphName = null;
    public override void EnterNamedWrappedGraph(TriGDocParser.NamedWrappedGraphContext context) =>
        GraphName = _resourceVisitor.Visit(context.labelOrSubject());

    public override void ExitNamedWrappedGraph(TriGDocParser.NamedWrappedGraphContext context) =>
        GraphName = null;
}

